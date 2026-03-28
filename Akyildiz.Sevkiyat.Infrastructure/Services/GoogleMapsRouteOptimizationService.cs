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

        public async Task<RouteOptimizationResultDto> OptimizeRouteAsync(
            List<string> addresses,
            string? startAddress,
            List<string> projectCodes,
            CancellationToken cancellationToken = default)
        {
            var excludedProjects = new List<string>();
            var validPairs = new List<(string Code, string Address)>();

            for (int i = 0; i < projectCodes.Count; i++)
            {
                var addr = i < addresses.Count ? addresses[i] : null;
                if (string.IsNullOrWhiteSpace(addr))
                    excludedProjects.Add(projectCodes[i]);
                else
                    validPairs.Add((projectCodes[i], addr));
            }

            if (validPairs.Count == 0)
            {
                return new RouteOptimizationResultDto(
                    new List<RouteStopDto>(), 0, 0, excludedProjects);
            }

            // Determine origin and waypoints
            string originAddress;
            List<(string Code, string Address)> waypointPairs;

            if (!string.IsNullOrWhiteSpace(startAddress))
            {
                originAddress = startAddress;
                waypointPairs = validPairs;
            }
            else
            {
                originAddress = validPairs[0].Address;
                waypointPairs = validPairs.Skip(1).ToList();
            }

            // destination = last waypoint (open route, no depot return)
            string destinationAddress;
            List<(string Code, string Address)> intermediatePairs;

            if (waypointPairs.Count == 0)
            {
                // only origin, no waypoints
                return new RouteOptimizationResultDto(
                    new List<RouteStopDto>
                    {
                        new(1, validPairs[0].Code, validPairs[0].Code, validPairs[0].Address, null, null)
                    },
                    0, 0, excludedProjects);
            }

            destinationAddress = waypointPairs[^1].Address;
            intermediatePairs  = waypointPairs.Take(waypointPairs.Count - 1).ToList();

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
                routeModifiers = new { avoidTolls = false, avoidHighways = false },
                languageCode = "tr-TR",
                units = "METRIC"
            };

            var url = $"https://routes.googleapis.com/directions/v2:computeRoutes?key={_apiKey}";
            _logger.LogInformation("Google Routes API çağrısı: {WaypointCount} durak", intermediatePairs.Count + 2);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = JsonContent.Create(requestBody);
            httpRequest.Headers.Add("X-Goog-FieldMask",
                "routes.duration,routes.distanceMeters,routes.optimizedIntermediateWaypointIndex,routes.legs");

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
                var durStr = dur.GetString() ?? "0s";
                totalDurationS = double.Parse(durStr.TrimEnd('s'));
            }

            // Build optimized stop order
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

            // Build stops list
            var stops = new List<RouteStopDto>();
            var legs = route.TryGetProperty("legs", out var legsEl) ? legsEl.EnumerateArray().ToList() : new List<JsonElement>();

            // Stop 1: origin
            // If startAddress was given, origin is a separate start point (not a project)
            // If not, origin is validPairs[0]
            int stopOrder = 1;
            if (string.IsNullOrWhiteSpace(startAddress))
            {
                stops.Add(new RouteStopDto(stopOrder++, validPairs[0].Code, validPairs[0].Code, validPairs[0].Address, null, null));
            }

            // Intermediates in optimized order
            for (int rank = 0; rank < optimizedOrder.Count; rank++)
            {
                var originalIdx = optimizedOrder[rank];
                var pair = intermediatePairs[originalIdx];
                var legIdx = string.IsNullOrWhiteSpace(startAddress) ? rank + 1 : rank + 1;
                double? legDist = legIdx < legs.Count && legs[legIdx].TryGetProperty("distanceMeters", out var legDistEl)
                    ? legDistEl.GetDouble() : null;
                double? legDur = legIdx < legs.Count ? GetLegDurationSeconds(legs[legIdx]) : null;
                stops.Add(new RouteStopDto(stopOrder++, pair.Code, pair.Code, pair.Address,
                    legDist.HasValue ? legDist / 1000.0 : null,
                    legDur.HasValue ? legDur / 60.0 : null));
            }

            // Last stop: destination
            {
                var lastLegIdx = legs.Count - 1;
                double? legDist = lastLegIdx >= 0 && legs[lastLegIdx].TryGetProperty("distanceMeters", out var ld2) ? ld2.GetDouble() : null;
                double? legDur = lastLegIdx >= 0 ? GetLegDurationSeconds(legs[lastLegIdx]) : null;
                var lastPair = waypointPairs[^1];
                stops.Add(new RouteStopDto(stopOrder, lastPair.Code, lastPair.Code, lastPair.Address,
                    legDist.HasValue ? legDist / 1000.0 : null,
                    legDur.HasValue ? legDur / 60.0 : null));
            }

            return new RouteOptimizationResultDto(
                stops,
                totalDistanceM / 1000.0,
                totalDurationS / 60.0,
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
