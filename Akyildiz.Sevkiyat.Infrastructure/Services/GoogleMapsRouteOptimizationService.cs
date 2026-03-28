using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;
using Akyildiz.Sevkiyat.Application.RouteOptimization.Interfaces;
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
                .Replace("  ", " ")
                .Replace("  ", " ")  // second pass for triple spaces
                .Trim();
        }

        // Bridge waypoints per vehicle type (Istanbul Bosphorus crossings)
        private static readonly Dictionary<string, (string Code, string Name, string Address)> BridgeWaypoints =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Kamyon"]   = ("__BRIDGE__", "Yavuz Sultan Selim Köprüsü", "Yavuz Sultan Selim Bridge, Istanbul, Turkey"),
                ["Kamyonet"] = ("__BRIDGE__", "Fatih Sultan Mehmet Köprüsü", "Fatih Sultan Mehmet Bridge, Istanbul, Turkey"),
            };

        public async Task<RouteOptimizationResultDto> OptimizeRouteAsync(
            List<string> addresses,
            string? startAddress,
            List<string> projectCodes,
            List<string> projectNames,
            string? vehicleType,
            bool forceBridgeCrossing,
            CancellationToken cancellationToken = default)
        {
            var excludedProjects = new List<string>();
            var validPairs = new List<(string Code, string Name, string Address)>();

            for (int i = 0; i < projectCodes.Count; i++)
            {
                var addr = i < addresses.Count ? addresses[i] : null;
                var name = i < projectNames.Count ? projectNames[i] : projectCodes[i];
                if (string.IsNullOrWhiteSpace(addr))
                    excludedProjects.Add(projectCodes[i]);
                else
                    validPairs.Add((projectCodes[i], name, NormalizeAddress(addr)));
            }

            if (validPairs.Count == 0)
            {
                return new RouteOptimizationResultDto(
                    new List<RouteStopDto>(), 0, 0, excludedProjects);
            }

            // TSP approach: origin = depot (or first project), destination = same as origin.
            // ALL projects are intermediates so Google optimizes the full order.
            // The "return to depot" last leg is excluded from output.
            string originAddress;
            bool originIsProject; // true when origin = first project (no dedicated depot)

            if (!string.IsNullOrWhiteSpace(startAddress))
            {
                originAddress = NormalizeAddress(startAddress);
                originIsProject = false;
            }
            else
            {
                originAddress = validPairs[0].Address;
                originIsProject = true;
            }

            // All valid projects become intermediates (Google optimizes all of them)
            var intermediatePairs = originIsProject
                ? validPairs.Skip(1).ToList()
                : validPairs.ToList();

            // Inject bridge waypoint if requested and vehicle type has a mapped bridge
            if (forceBridgeCrossing
                && !string.IsNullOrWhiteSpace(vehicleType)
                && BridgeWaypoints.TryGetValue(vehicleType, out var bridge))
            {
                // Insert at position 0 — Google will reorder it to the optimal crossing point
                intermediatePairs.Insert(0, bridge);
                _logger.LogInformation("Köprü geçiş noktası eklendi: {Bridge}", bridge.Name);
            }

            // Destination = same as origin (round trip / TSP; last leg will be dropped)
            var destinationAddress = originAddress;

            if (intermediatePairs.Count == 0)
            {
                // Single project, nothing to optimize
                return new RouteOptimizationResultDto(
                    new List<RouteStopDto>
                    {
                        new(1, validPairs[0].Code, validPairs[0].Name, validPairs[0].Address, null, null)
                    },
                    0, 0, excludedProjects);
            }

            // vehicleType → routeModifiers
            // Kamyon: heavy vehicle profile (avoidFerries added)
            // Kamyonet / Minibüs: standard car profile
            bool isKamyon = string.Equals(vehicleType, "Kamyon", StringComparison.OrdinalIgnoreCase)
                         || string.IsNullOrWhiteSpace(vehicleType); // default

            object routeModifiers = isKamyon
                ? new { avoidTolls = false, avoidFerries = false }
                : new { avoidTolls = false };

            // Build request body
            var requestBody = new
            {
                origin = new { address = originAddress },
                destination = new { address = destinationAddress },
                intermediates = intermediatePairs.Select(p => new
                {
                    address = p.Address
                }).ToArray(),
                travelMode = "DRIVE",
                optimizeWaypointOrder = true,
                computeAlternativeRoutes = false,
                routeModifiers,
                languageCode = "tr-TR",
                units = "METRIC"
            };

            var url = $"https://routes.googleapis.com/directions/v2:computeRoutes?key={_apiKey}";
            _logger.LogInformation("Google Routes API çağrısı: {WaypointCount} durak, araç tipi: {VehicleType}",
                intermediatePairs.Count + 2, vehicleType ?? "Kamyon");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = JsonContent.Create(requestBody);
            // Explicit field mask — routes.legs alone does NOT return sub-fields
            httpRequest.Headers.Add("X-Goog-FieldMask",
                "routes.distanceMeters,routes.duration,routes.optimizedIntermediateWaypointIndex," +
                "routes.legs.distanceMeters,routes.legs.duration");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Google Routes API yanıtı: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Routes API hatası: {StatusCode} {Body}", response.StatusCode, rawJson);
                throw new DomainException($"Google Routes API hatası: {(int)response.StatusCode} — Rota hesaplanamadı. API anahtarını veya proje adreslerini kontrol edin.");
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
                // duration is returned as "1234s" string
                var durStr = dur.GetString() ?? "0s";
                totalDurationS = double.TryParse(durStr.TrimEnd('s'), out var ds) ? ds : 0;
            }

            // Build optimized stop order for intermediates
            var optimizedOrder = new List<int>();
            if (route.TryGetProperty("optimizedIntermediateWaypointIndex", out var orderArr))
            {
                foreach (var idx in orderArr.EnumerateArray())
                    optimizedOrder.Add(idx.GetInt32());
            }
            else
            {
                // No reordering — sequential
                for (int i = 0; i < intermediatePairs.Count; i++)
                    optimizedOrder.Add(i);
            }

            // Legs array: leg[k] is the segment arriving at the (k+1)-th stop in optimized order.
            // leg[0]: origin → 1st optimized intermediate (or destination if no intermediates)
            // leg[k]: k-th → (k+1)-th optimized stop
            // leg[N]: last intermediate → destination
            var legs = route.TryGetProperty("legs", out var legsEl)
                ? legsEl.EnumerateArray().ToList()
                : new List<JsonElement>();

            var stops = new List<RouteStopDto>();
            int stopOrder = 1;

            // Stop 1: origin project (no incoming leg, so null distance)
            if (originIsProject)
            {
                stops.Add(new RouteStopDto(stopOrder++, validPairs[0].Code, validPairs[0].Name, validPairs[0].Address, null, null));
            }

            // Intermediates in optimized order.
            // leg[rank] arrives at the (rank)-th optimized intermediate.
            // The LAST leg (legs[N]) is origin→return, which we discard.
            for (int rank = 0; rank < optimizedOrder.Count; rank++)
            {
                var originalIdx = optimizedOrder[rank];
                var pair = intermediatePairs[originalIdx];
                var legIdx = rank; // leg[rank] delivers us to this stop
                double? legDist = legIdx < legs.Count && legs[legIdx].TryGetProperty("distanceMeters", out var legDistEl)
                    ? legDistEl.GetDouble() : null;
                double? legDur = legIdx < legs.Count ? GetLegDurationSeconds(legs[legIdx]) : null;
                stops.Add(new RouteStopDto(stopOrder++, pair.Code, pair.Name, pair.Address,
                    legDist.HasValue ? legDist / 1000.0 : null,
                    legDur.HasValue ? legDur / 60.0 : null));
            }

            // Total distance/duration: subtract the last (return-to-depot) leg
            // so the displayed totals reflect only the delivery route.
            double returnLegDistM = 0, returnLegDurS = 0;
            if (legs.Count > 0)
            {
                var returnLeg = legs[^1];
                if (returnLeg.TryGetProperty("distanceMeters", out var rld)) returnLegDistM = rld.GetDouble();
                returnLegDurS = GetLegDurationSeconds(returnLeg) ?? 0;
            }

            return new RouteOptimizationResultDto(
                stops,
                (totalDistanceM - returnLegDistM) / 1000.0,
                (totalDurationS - returnLegDurS) / 60.0,
                excludedProjects);
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
