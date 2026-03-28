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

        private const int MaxWaypoints = 25; // origin + non-via intermediates + destination

        // Bridge via-waypoint coordinates (lat/lng pins on the bridge mid-span)
        private static readonly (double Lat, double Lon) YavuzSultanSelim   = (41.2027, 29.0656);
        private static readonly (double Lat, double Lon) FatihSultanMehmet  = (41.0882, 29.0594);

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

        private static object BuildWaypointFromStop(RouteOrderingService.StopInfo stop)
        {
            if (stop.Code == "__BRIDGE__")
            {
                // Via waypoint — not a stop, just forces the route through the bridge.
                // "via: true" means no leg boundary is created for this waypoint.
                var coords = stop.Name.Contains("Yavuz") ? YavuzSultanSelim : FatihSultanMehmet;
                return new
                {
                    via = true,
                    location = new { latLng = new { latitude = coords.Lat, longitude = coords.Lon } }
                };
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
                    request.VehicleType, request.ForceBridgeCrossing, request.ReturnToStart);

                orderedStops = ordered.Stops;
                bridgeNotice = ordered.BridgeNotice;
                hasBridge = ordered.HasBridgeCrossing;

                // Waypoint limit check — via waypoints (bridge) do NOT count against the limit
                int totalWps = 1 + orderedStops.Count(s => s.Code != "__BRIDGE__") + 1;
                if (request.ReturnToStart) totalWps += 1;
                if (totalWps > MaxWaypoints)
                    throw new DomainException($"Maksimum {MaxWaypoints} durak desteklenmektedir. Köprü geçişi 1 durak sayılır. Mevcut: {totalWps}");
            }
            else
            {
                // Legacy: keep original order (Google reorders with optimizeWaypointOrder=true)
                orderedStops = originIsProject ? validStops.Skip(1).ToList() : validStops.ToList();
                if (request.ForceBridgeCrossing && !string.IsNullOrWhiteSpace(request.VehicleType))
                    bridgeNotice = null; // legacy: just show UI notice
            }

            // ── Build Google Routes API request ───────────────────────────────
            bool isKamyon = string.Equals(request.VehicleType, "Kamyon", StringComparison.OrdinalIgnoreCase)
                         || string.IsNullOrWhiteSpace(request.VehicleType);

            // avoidFerries: true — ferries could be alternative Bosphorus crossings (Kabataş-Üsküdar etc.)
            // avoidTolls: false — bridges are tolled, we still want to use them
            // avoidHighways: false — highways are needed for truck routes
            object routeModifiers = new { avoidTolls = false, avoidHighways = false, avoidFerries = true };

            // For new ordering: intermediates = orderedStops in order (no reorder by Google)
            // For legacy: all valid stops as intermediates (Google reorders)
            // destination is always same as origin (TSP round-trip approach)
            List<RouteOrderingService.StopInfo> intermediates = orderedStops;

            bool optimizeWaypointOrder = !useNewOrdering; // false = backend-ordered

            if (intermediates.Count == 0)
            {
                return new RouteOptimizationResultDto(
                    new List<RouteStopDto>
                    {
                        new(1, validStops[0].Code, validStops[0].Name, validStops[0].Address, null, null)
                    },
                    0, 0, excludedProjects, bridgeNotice);
            }

            // Build origin/destination waypoints — use latLng when coordinates are available
            // destination is always same as origin (TSP round-trip trick)
            var originWaypoint = BuildWaypoint(originAddress, startLat, startLon);

            var requestBody = new
            {
                origin      = originWaypoint,
                destination = originWaypoint,
                intermediates = intermediates.Select(p => BuildWaypointFromStop(p)).ToArray(),
                travelMode = "DRIVE",
                optimizeWaypointOrder,
                computeAlternativeRoutes = false,
                routeModifiers,
                languageCode = "tr-TR",
                units = "METRIC"
            };

            var url = $"https://routes.googleapis.com/directions/v2:computeRoutes?key={_apiKey}";
            _logger.LogInformation("Google Routes API çağrısı: {Count} durak, araç: {Vehicle}, optimize: {Opt}",
                intermediates.Count + 2, request.VehicleType ?? "Kamyon", optimizeWaypointOrder);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = JsonContent.Create(requestBody);
            httpRequest.Headers.Add("X-Goog-FieldMask",
                "routes.distanceMeters,routes.duration,routes.optimizedIntermediateWaypointIndex," +
                "routes.legs.distanceMeters,routes.legs.duration");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Google Routes API yanıtı: {Status}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Routes API hatası: {Status} {Body}", response.StatusCode, rawJson);
                throw new DomainException($"Google Routes API hatası: {(int)response.StatusCode} — Rota hesaplanamadı.");
            }

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
                throw new DomainException("Google Routes API geçerli rota döndürmedi. Adreslerin doğruluğunu kontrol edin.");

            var route = routes[0];

            double totalDistanceM = route.TryGetProperty("distanceMeters", out var dm) ? dm.GetDouble() : 0;
            double totalDurationS = 0;
            if (route.TryGetProperty("duration", out var dur))
            {
                var durStr = dur.GetString() ?? "0s";
                totalDurationS = double.TryParse(durStr.TrimEnd('s'), out var ds) ? ds : 0;
            }

            // Optimized order (legacy only; new ordering returns identity)
            var optimizedOrder = new List<int>();
            if (!useNewOrdering && route.TryGetProperty("optimizedIntermediateWaypointIndex", out var orderArr))
            {
                foreach (var idx in orderArr.EnumerateArray())
                    optimizedOrder.Add(idx.GetInt32());
            }
            else
            {
                for (int i = 0; i < intermediates.Count; i++) optimizedOrder.Add(i);
            }

            var legs = route.TryGetProperty("legs", out var legsEl)
                ? legsEl.EnumerateArray().ToList()
                : new List<JsonElement>();

            var stops = new List<RouteStopDto>();
            int stopOrder = 1;

            // Stop 1: origin project (legacy, no incoming leg)
            if (!useNewOrdering && originIsProject)
                stops.Add(new RouteStopDto(stopOrder++, validStops[0].Code, validStops[0].Name, validStops[0].Address, null, null));

            // Via waypoints (bridges) don't create leg boundaries — track leg index separately.
            // legs.Count = non-via intermediates + 1 (return leg)
            int legIndex = 0;
            for (int rank = 0; rank < optimizedOrder.Count; rank++)
            {
                var originalIdx = optimizedOrder[rank];
                var pair = intermediates[originalIdx];

                if (pair.Code == "__BRIDGE__")
                {
                    // Via waypoint — no leg boundary, don't advance legIndex
                    continue;
                }

                double? legDist = legIndex < legs.Count && legs[legIndex].TryGetProperty("distanceMeters", out var ldEl)
                    ? ldEl.GetDouble() : null;
                double? legDur = legIndex < legs.Count ? GetLegDurationSeconds(legs[legIndex]) : null;

                stops.Add(new RouteStopDto(stopOrder++, pair.Code, pair.Name, pair.Address,
                    legDist.HasValue ? legDist / 1000.0 : null,
                    legDur.HasValue ? legDur / 60.0 : null));
                legIndex++;
            }

            // Subtract return-to-depot leg from totals
            double returnLegDistM = 0, returnLegDurS = 0;
            if (legs.Count > 0)
            {
                var returnLeg = legs[^1];
                if (returnLeg.TryGetProperty("distanceMeters", out var rld)) returnLegDistM = rld.GetDouble();
                returnLegDurS = GetLegDurationSeconds(returnLeg) ?? 0;
            }

            // Only subtract return leg if NOT ReturnToStart (TSP round-trip subtraction)
            double finalDistKm = !request.ReturnToStart
                ? (totalDistanceM - returnLegDistM) / 1000.0
                : totalDistanceM / 1000.0;
            double finalDurMin = !request.ReturnToStart
                ? (totalDurationS - returnLegDurS) / 60.0
                : totalDurationS / 60.0;

            // ── Time window warnings ──────────────────────────────────────────
            var effectiveDepartureTime = (request.DepartureTime is { } dt && dt != default)
                ? dt : new TimeOnly(8, 0);
            var timeWindowWarnings = BuildTimeWindowWarnings(
                stops, intermediates, legs, optimizedOrder, effectiveDepartureTime);

            return new RouteOptimizationResultDto(
                stops,
                finalDistKm,
                finalDurMin,
                excludedProjects,
                bridgeNotice,
                timeWindowWarnings.Count > 0 ? timeWindowWarnings : null);
        }

        private List<TimeWindowWarningDto> BuildTimeWindowWarnings(
            List<RouteStopDto> stops,
            List<RouteOrderingService.StopInfo> intermediates,
            List<JsonElement> legs,
            List<int> optimizedOrder,
            TimeOnly departureTime)
        {
            var warnings = new List<TimeWindowWarningDto>();
            const double dwellMinutes = 15.0;

            // Guard: if DepartureTime came as 00:00 (TimeOnly default / deserialization fallback) use 08:00
            var effectiveDeparture = (departureTime == default) ? new TimeOnly(8, 0) : departureTime;

            // Track absolute minutes-since-midnight so we don't confuse relative and absolute offsets
            double currentMinutes = effectiveDeparture.Hour * 60.0 + effectiveDeparture.Minute;
            int legIdx = 0;

            for (int rank = 0; rank < optimizedOrder.Count; rank++)
            {
                var originalIdx = optimizedOrder[rank];
                if (originalIdx >= intermediates.Count) continue;
                var stop = intermediates[originalIdx];

                if (stop.Code == "__BRIDGE__") continue; // via waypoint — no leg boundary

                // Arrival = current position in time + travel leg duration
                double legDurMin = legIdx < legs.Count ? (GetLegDurationSeconds(legs[legIdx]) ?? 0) / 60.0 : 0;
                legIdx++;
                currentMinutes += legDurMin;

                if (stop.DeliveryWindowStart.HasValue && stop.DeliveryWindowEnd.HasValue)
                {
                    var arrivalTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(currentMinutes % (24 * 60)));
                    bool isLate = arrivalTime > stop.DeliveryWindowEnd.Value;

                    if (isLate)
                    {
                        warnings.Add(new TimeWindowWarningDto(
                            stop.Code,
                            stop.Name,
                            stop.DeliveryWindowStart.Value,
                            stop.DeliveryWindowEnd.Value,
                            arrivalTime,
                            true));
                    }
                }

                // Dwell at this stop — add AFTER arrival so it feeds into next stop's travel start
                currentMinutes += dwellMinutes;
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
