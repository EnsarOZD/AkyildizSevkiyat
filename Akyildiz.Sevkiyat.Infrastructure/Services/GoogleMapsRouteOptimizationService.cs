using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Interfaces;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Services;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Infrastructure.Services
{
    public class GoogleMapsRouteOptimizationService : IRouteOptimizationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GoogleMapsRouteOptimizationService> _logger;
        private readonly RouteOrderingService _ordering = new();

        private const int MaxWaypoints = 50; // origin + non-via intermediates + destination (Google Routes API v2 limit: 25 intermediates)

        // Bridge waypoint coordinates.
        // YSS: köprü ortası — çalışıyor.
        // FSM: 3 aday sırayla denenir; ilk boş rota döndürende sonrakine geçilir.
        private static readonly (double Lat, double Lon) YavuzSultanSelim = (41.2027, 29.0656);

        // FSM adayları: (lat, lon, via). via=false = normal stop, via=true = pass-through waypoint.
        private static readonly (double Lat, double Lon, bool Via)[] FsmCandidates =
        [
            (41.0773, 29.0542, false),   // köprü ortası — birincil aday
            (41.0741, 29.0689, false),   // Anadolu giriş noktası
            (41.0800, 29.0500, false),   // alternatif merkez
            (41.0773, 29.0542, true),    // birincil + via=true
            (41.0741, 29.0689, true),    // Anadolu giriş + via=true
        ];

        public GoogleMapsRouteOptimizationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GoogleMapsRouteOptimizationService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GoogleMaps:ApiKey"] ?? throw new DomainException("GoogleMaps:ApiKey yapılandırması eksik.");
            _logger = logger;
        }

        // ── Address normalization ─────────────────────────────────────────────
        private static string NormalizeAddress(string address)
        {
            return address
                .Replace("\\", "/")
                .Replace("NO:", "No ")
                .Replace("MAH.", "Mah.")
                .Replace("CAD.", "Cad.")
                .Replace("SOK.", "Sok.")
                .Replace("   ", " ")
                .Replace("  ", " ")
                .Trim();
        }

        // ── Waypoint builder — Routes API v2 requires latLng for coordinates ──
        private static object BuildWaypoint(string? address, double? lat, double? lon)
        {
            if (lat.HasValue && lon.HasValue)
                return new { location = new { latLng = new { latitude = lat.Value, longitude = lon.Value } } };
            return new { address };
        }

        private static object BuildWaypointFromStop(
            RouteOrderingService.StopInfo stop,
            (double Lat, double Lon, bool Via)? fsmOverride = null)
        {
            if (stop.Code == "__BRIDGE__")
            {
                if (stop.Name.Contains("Yavuz"))
                {
                    // YSS — via=false (normal waypoint; via=true also works but normal is more reliable)
                    return new
                    {
                        via = false,
                        location = new { latLng = new { latitude = YavuzSultanSelim.Lat, longitude = YavuzSultanSelim.Lon } }
                    };
                }
                else
                {
                    // FSM — use the provided candidate
                    var c = fsmOverride ?? FsmCandidates[0];
                    return new
                    {
                        via = c.Via,
                        location = new { latLng = new { latitude = c.Lat, longitude = c.Lon } }
                    };
                }
            }
            if (stop.Latitude.HasValue && stop.Longitude.HasValue)
                return new { location = new { latLng = new { latitude = stop.Latitude.Value, longitude = stop.Longitude.Value } } };
            return new { address = stop.Address };
        }

        public async Task<RouteOptimizationResultDto> OptimizeRouteAsync(
            RouteOptimizationRequestDto request,
            List<string> addresses,
            List<string> projectCodes,
            List<string> projectNames,
            List<double?> latitudes,
            List<double?> longitudes,
            List<TimeOnly?> deliveryWindowStarts,
            List<TimeOnly?> deliveryWindowEnds,
            CancellationToken cancellationToken = default)
        {
            var excludedProjects = new List<string>();
            var validStops = new List<RouteOrderingService.StopInfo>();

            for (int i = 0; i < projectCodes.Count; i++)
            {
                var addr = i < addresses.Count ? addresses[i] : null;
                var name = i < projectNames.Count ? projectNames[i] : projectCodes[i];
                if (string.IsNullOrWhiteSpace(addr))
                {
                    excludedProjects.Add(projectCodes[i]);
                    continue;
                }
                validStops.Add(new RouteOrderingService.StopInfo(
                    projectCodes[i],
                    name,
                    NormalizeAddress(addr),
                    i < latitudes.Count ? latitudes[i] : null,
                    i < longitudes.Count ? longitudes[i] : null,
                    i < deliveryWindowStarts.Count ? deliveryWindowStarts[i] : null,
                    i < deliveryWindowEnds.Count ? deliveryWindowEnds[i] : null
                ));
            }

            if (validStops.Count == 0)
                return new RouteOptimizationResultDto(new List<RouteStopDto>(), 0, 0, excludedProjects, null);

            // ── Determine start point ─────────────────────────────────────────
            bool useNewOrdering = request.StartLocationType.HasValue;
            string? originAddress = null;
            double? startLat = request.StartLatitude;
            double? startLon = request.StartLongitude;

            if (useNewOrdering)
            {
                // StartAddress for ManualAddress case (already geocoded by frontend)
                if (!string.IsNullOrWhiteSpace(request.StartAddress))
                    originAddress = NormalizeAddress(request.StartAddress);
                // For Depot/CurrentLocation: lat/lon will be used directly in waypoint (no address needed)
            }
            else
            {
                // Legacy behaviour
                if (!string.IsNullOrWhiteSpace(request.StartAddress))
                    originAddress = NormalizeAddress(request.StartAddress);
            }

            bool originIsProject = !useNewOrdering && originAddress == null;
            if (originIsProject)
            {
                originAddress = validStops[0].Address;
                startLat = null;
                startLon = null;
            }
            else if (useNewOrdering && originAddress == null && !startLat.HasValue)
            {
                // No start defined at all — fall back to first project
                originIsProject = true;
                originAddress = validStops[0].Address;
            }

            // ── New ordering algorithm ────────────────────────────────────────
            List<RouteOrderingService.StopInfo> orderedStops;
            string? bridgeNotice = null;
            bool hasBridge = false;

            if (useNewOrdering)
            {
                var ordered = _ordering.OrderStops(
                    validStops, startLat, startLon,
                    request.VehicleType, request.ForceBridgeCrossing, request.ReturnToStart,
                    request.DepartureTime);

                orderedStops = ordered.Stops;
                bridgeNotice = ordered.BridgeNotice;
                hasBridge = ordered.HasBridgeCrossing;

                // Note: Google Routes API v2 supports up to 25 intermediate waypoints.
                // Via waypoints (bridge) do NOT count against the limit.
            }
            else
            {
                // Legacy: keep original order (Google reorders with optimizeWaypointOrder=true)
                orderedStops = originIsProject ? validStops.Skip(1).ToList() : validStops.ToList();
                if (request.ForceBridgeCrossing && !string.IsNullOrWhiteSpace(request.VehicleType))
                    bridgeNotice = null; // legacy: just show UI notice
            }

            // ── Build Google Routes API request ───────────────────────────────
            // avoidFerries: only within Istanbul metro area (lon < 29.3).
            // For Kocaeli/Gebze depots (lon ≥ 29.3) avoidFerries can block the return leg
            // (Google may want to route via Gulf-of-Izmit ferry shortcuts) and return {}.
            // When bridge via-waypoints are used, ferry avoidance is redundant anyway.
            bool avoidFerries = !startLon.HasValue || startLon.Value < 29.3;
            object routeModifiers = new { avoidTolls = false, avoidHighways = false, avoidFerries };

            // For new ordering: intermediates = orderedStops in order (no reorder by Google)
            List<RouteOrderingService.StopInfo> intermediates = orderedStops;
            bool optimizeWaypointOrder = !useNewOrdering;

            if (intermediates.Count == 0)
            {
                return new RouteOptimizationResultDto(
                    new List<RouteStopDto>
                    {
                        new(1, validStops[0].Code, validStops[0].Name, validStops[0].Address, null, null,
                            validStops[0].Latitude, validStops[0].Longitude)
                    },
                    0, 0, excludedProjects, bridgeNotice,
                    null, startLat, startLon);
            }

            // ── DÜZELTME 1&2: Destination = last real stop (ReturnToStart=false)
            //                              or origin           (ReturnToStart=true) ──────
            RouteOrderingService.StopInfo? destinationStop = null;
            List<RouteOrderingService.StopInfo> requestIntermediates;

            if (!request.ReturnToStart)
            {
                // Find the last non-bridge stop; it becomes destination
                int lastRealIdx = -1;
                for (int i = intermediates.Count - 1; i >= 0; i--)
                {
                    if (intermediates[i].Code != "__BRIDGE__") { lastRealIdx = i; break; }
                }
                if (lastRealIdx >= 0)
                {
                    destinationStop = intermediates[lastRealIdx];
                    requestIntermediates = intermediates.Where((_, i) => i != lastRealIdx).ToList();
                }
                else
                {
                    requestIntermediates = intermediates;
                }
            }
            else
            {
                requestIntermediates = intermediates;
            }

            // ── DÜZELTME 3: origin == destination (ReturnToStart=true) ile durak yoksa hata ──
            if (request.ReturnToStart && requestIntermediates.Count(s => s.Code != "__BRIDGE__") == 0)
                throw new DomainException("Rota hesaplanamadı: Başlangıç ve bitiş noktası aynı, en az 1 durak gereklidir.");

            var originWaypoint = BuildWaypoint(originAddress, startLat, startLon);

            // FSM fallback zinciri: köprü varsa sırayla FsmCandidates denenir;
            // köprü yoksa tek deneme (fsmOverride = null, ilk aday kullanılır).
            bool hasFsm = hasBridge && requestIntermediates.Any(s => s.Code == "__BRIDGE__" && !s.Name.Contains("Yavuz"));
            var candidatesToTry = hasFsm
                ? FsmCandidates.Select(c => ((double Lat, double Lon, bool Via)?)c).ToList()
                : new List<(double Lat, double Lon, bool Via)?> { null };

            var url = $"https://routes.googleapis.com/directions/v2:computeRoutes?key={_apiKey}";
            _logger.LogInformation("Google Routes API çağrısı: {Count} durak, araç: {Vehicle}, optimize: {Opt}, FSM aday sayısı: {Fsm}",
                requestIntermediates.Count + 2, request.VehicleType ?? "Kamyon", optimizeWaypointOrder,
                hasFsm ? FsmCandidates.Length : 0);

            string rawJson = string.Empty;
            string requestBodyJson = string.Empty;
            JsonDocument doc = null!;

            foreach (var fsmOverride in candidatesToTry)
            {
                var destinationWaypoint = destinationStop != null
                    ? BuildWaypointFromStop(destinationStop, fsmOverride)
                    : originWaypoint;

                var requestBody = new
                {
                    origin        = originWaypoint,
                    destination   = destinationWaypoint,
                    intermediates = requestIntermediates.Select(p => BuildWaypointFromStop(p, fsmOverride)).ToArray(),
                    travelMode = "DRIVE",
                    optimizeWaypointOrder,
                    computeAlternativeRoutes = false,
                    routeModifiers,
                    languageCode = "tr-TR",
                    units = "METRIC"
                };

                requestBodyJson = System.Text.Json.JsonSerializer.Serialize(requestBody,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = false });

                if (fsmOverride.HasValue)
                    _logger.LogInformation("FSM deneniyor: lat={Lat}, lon={Lon}, via={Via}",
                        fsmOverride.Value.Item1, fsmOverride.Value.Item2, fsmOverride.Value.Item3);

                try
                {
                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                    httpRequest.Content = JsonContent.Create(requestBody);
                    httpRequest.Headers.Add("X-Goog-FieldMask",
                        "routes.distanceMeters,routes.duration,routes.optimizedIntermediateWaypointIndex," +
                        "routes.legs.distanceMeters,routes.legs.duration");

                    using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                    rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogInformation("Google Routes API yanıtı: {Status}", response.StatusCode);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Google Routes API hatası: {Status} {Body}", response.StatusCode, rawJson);
                        var snippet = rawJson.Length > 400 ? rawJson[..400] : rawJson;
                        throw new DomainException($"Google Routes API hatası: {(int)response.StatusCode} — {snippet}");
                    }

                    var parsed = JsonDocument.Parse(rawJson);
                    var root2 = parsed.RootElement;

                    if (!root2.TryGetProperty("routes", out var routes2) || routes2.GetArrayLength() == 0)
                    {
                        parsed.Dispose();
                        if (fsmOverride.HasValue)
                            _logger.LogWarning("FSM adayı boş rota döndürdü: lat={Lat}, lon={Lon}, via={Via} | Yanıt: {Body}",
                                fsmOverride.Value.Item1, fsmOverride.Value.Item2, fsmOverride.Value.Item3, rawJson);
                        else
                            _logger.LogWarning("Google Routes API boş rota döndürdü. İstek: {Req} | Yanıt: {Body}", requestBodyJson, rawJson);
                        continue; // sonraki adaya geç
                    }

                    if (fsmOverride.HasValue)
                        _logger.LogInformation("FSM başarılı: lat={Lat}, lon={Lon}, via={Via}",
                            fsmOverride.Value.Item1, fsmOverride.Value.Item2, fsmOverride.Value.Item3);

                    doc = parsed;
                    break;
                }
                catch (DomainException) { throw; }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Google Routes API bağlantı hatası");
                    throw new DomainException("Google Routes API'ye bağlanılamadı. İnternet bağlantısını kontrol edin.");
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Google Routes API zaman aşımı");
                    throw new DomainException("Google Routes API zaman aşımına uğradı. Lütfen tekrar deneyin.");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Google Routes API JSON parse hatası. Yanıt: {Raw}", rawJson);
                    throw new DomainException("Google Routes API geçersiz yanıt döndürdü.");
                }
            }

            if (doc is null)
            {
                _logger.LogWarning("Tüm FSM adayları boş rota döndürdü. Son istek: {Req}", requestBodyJson);
                throw new DomainException($"Google Routes API geçerli rota döndürmedi. İstek: {requestBodyJson}");
            }

            using (doc)
            {
            var root = doc.RootElement;
            var routes = root.GetProperty("routes");

            var route = routes[0];

            double totalDistanceM = route.TryGetProperty("distanceMeters", out var dm) ? dm.GetDouble() : 0;
            double totalDurationS = 0;
            if (route.TryGetProperty("duration", out var dur))
            {
                var durStr = dur.GetString() ?? "0s";
                totalDurationS = double.TryParse(durStr.TrimEnd('s'), out var ds) ? ds : 0;
            }

            // Optimized order — indices into requestIntermediates
            var optimizedOrder = new List<int>();
            if (!useNewOrdering && route.TryGetProperty("optimizedIntermediateWaypointIndex", out var orderArr))
            {
                foreach (var idx in orderArr.EnumerateArray())
                    optimizedOrder.Add(idx.GetInt32());
            }
            else
            {
                for (int i = 0; i < requestIntermediates.Count; i++) optimizedOrder.Add(i);
            }

            var legs = route.TryGetProperty("legs", out var legsEl)
                ? legsEl.EnumerateArray().ToList()
                : new List<JsonElement>();

            var stops = new List<RouteStopDto>();
            int stopOrder = 1;

            // Stop 1: origin project (legacy, no incoming leg)
            if (!useNewOrdering && originIsProject)
                stops.Add(new RouteStopDto(stopOrder++, validStops[0].Code, validStops[0].Name, validStops[0].Address, null, null,
                    validStops[0].Latitude, validStops[0].Longitude));

            // Via waypoints don't create leg boundaries — track legIndex separately
            int legIndex = 0;
            for (int rank = 0; rank < optimizedOrder.Count; rank++)
            {
                var originalIdx = optimizedOrder[rank];
                var pair = requestIntermediates[originalIdx];

                if (pair.Code == "__BRIDGE__") continue;

                double? legDist = legIndex < legs.Count && legs[legIndex].TryGetProperty("distanceMeters", out var ldEl)
                    ? ldEl.GetDouble() : null;
                double? legDur = legIndex < legs.Count ? GetLegDurationSeconds(legs[legIndex]) : null;

                stops.Add(new RouteStopDto(stopOrder++, pair.Code, pair.Name, pair.Address,
                    legDist.HasValue ? legDist / 1000.0 : null,
                    legDur.HasValue ? legDur / 60.0 : null,
                    pair.Latitude, pair.Longitude));
                legIndex++;
            }

            // For ReturnToStart=false: destinationStop is the last leg's endpoint
            if (destinationStop != null)
            {
                double? legDist = legIndex < legs.Count && legs[legIndex].TryGetProperty("distanceMeters", out var ldEl2)
                    ? ldEl2.GetDouble() : null;
                double? legDur = legIndex < legs.Count ? GetLegDurationSeconds(legs[legIndex]) : null;
                stops.Add(new RouteStopDto(stopOrder++, destinationStop.Code, destinationStop.Name, destinationStop.Address,
                    legDist.HasValue ? legDist / 1000.0 : null,
                    legDur.HasValue ? legDur / 60.0 : null,
                    destinationStop.Latitude, destinationStop.Longitude));
            }

            // Total distance/duration:
            // ReturnToStart=false → totalDistanceM = origin→last stop (no return leg in request)
            // ReturnToStart=true  → totalDistanceM = full round trip (return leg included)
            double finalDistKm = totalDistanceM / 1000.0;
            double finalDurMin = totalDurationS / 60.0;

            // ── Time window warnings ──────────────────────────────────────────
            var effectiveDepartureTime = (request.DepartureTime is { } dt && dt != default)
                ? dt : new TimeOnly(8, 0);
            var timeWindowWarnings = BuildTimeWindowWarnings(
                requestIntermediates, destinationStop, legs, optimizedOrder, effectiveDepartureTime);

            return new RouteOptimizationResultDto(
                stops,
                finalDistKm,
                finalDurMin,
                excludedProjects,
                bridgeNotice,
                timeWindowWarnings.Count > 0 ? timeWindowWarnings : null,
                startLat,
                startLon);
            } // end using (doc)
        }

        private List<TimeWindowWarningDto> BuildTimeWindowWarnings(
            List<RouteOrderingService.StopInfo> requestIntermediates,
            RouteOrderingService.StopInfo? destinationStop,
            List<JsonElement> legs,
            List<int> optimizedOrder,
            TimeOnly departureTime)
        {
            var warnings = new List<TimeWindowWarningDto>();
            const double dwellMinutes = 15.0;

            var effectiveDeparture = (departureTime == default) ? new TimeOnly(8, 0) : departureTime;
            double currentMinutes = effectiveDeparture.Hour * 60.0 + effectiveDeparture.Minute;
            int legIdx = 0;

            void CheckWindow(RouteOrderingService.StopInfo stop, bool addDwell)
            {
                if (stop.DeliveryWindowStart.HasValue && stop.DeliveryWindowEnd.HasValue)
                {
                    var arrivalTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(currentMinutes % (24 * 60)));
                    if (arrivalTime < stop.DeliveryWindowStart.Value)
                        warnings.Add(new TimeWindowWarningDto(
                            stop.Code, stop.Name,
                            stop.DeliveryWindowStart.Value, stop.DeliveryWindowEnd.Value,
                            arrivalTime, false, "EarlyArrival"));
                    else if (arrivalTime > stop.DeliveryWindowEnd.Value)
                        warnings.Add(new TimeWindowWarningDto(
                            stop.Code, stop.Name,
                            stop.DeliveryWindowStart.Value, stop.DeliveryWindowEnd.Value,
                            arrivalTime, true, "LateArrival"));
                }
                if (addDwell) currentMinutes += dwellMinutes;
            }

            for (int rank = 0; rank < optimizedOrder.Count; rank++)
            {
                var originalIdx = optimizedOrder[rank];
                if (originalIdx >= requestIntermediates.Count) continue;
                var stop = requestIntermediates[originalIdx];

                if (stop.Code == "__BRIDGE__") continue;

                double legDurMin = legIdx < legs.Count ? (GetLegDurationSeconds(legs[legIdx]) ?? 0) / 60.0 : 0;
                legIdx++;
                currentMinutes += legDurMin;
                CheckWindow(stop, addDwell: true);
            }

            // Check destination stop (ReturnToStart=false only)
            if (destinationStop != null && destinationStop.Code != "__BRIDGE__")
            {
                double legDurMin = legIdx < legs.Count ? (GetLegDurationSeconds(legs[legIdx]) ?? 0) / 60.0 : 0;
                currentMinutes += legDurMin;
                CheckWindow(destinationStop, addDwell: false);
            }

            return warnings;
        }

        private static double? GetLegDurationSeconds(JsonElement leg)
        {
            if (leg.ValueKind == JsonValueKind.Undefined) return null;
            if (!leg.TryGetProperty("duration", out var dur)) return null;
            var s = dur.GetString() ?? "0s";
            return double.TryParse(s.TrimEnd('s'), out var v) ? v : null;
        }
    }
}
