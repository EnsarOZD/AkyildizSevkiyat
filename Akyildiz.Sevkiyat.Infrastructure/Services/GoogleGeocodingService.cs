using System.Text.Json;
using System.Text.RegularExpressions;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Akyildiz.Sevkiyat.Infrastructure.Services
{
    public class GoogleGeocodingService : IGeocodingService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public GoogleGeocodingService(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _apiKey = configuration["GoogleMaps:ApiKey"]
                ?? throw new InvalidOperationException("GoogleMaps:ApiKey yapılandırması eksik.");
        }

        public async Task<(double Lat, double Lng)?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            var normalized = NormalizeAddress(address);
            var url = "https://maps.googleapis.com/maps/api/geocode/json" +
                      $"?address={Uri.EscapeDataString(normalized)}" +
                      $"&key={_apiKey}" +
                      "&language=tr" +
                      "&region=tr";

            using var response = await _http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = doc.RootElement;
            var status = root.GetProperty("status").GetString();

            if (status != "OK") return null;

            var location = root
                .GetProperty("results")[0]
                .GetProperty("geometry")
                .GetProperty("location");

            return (location.GetProperty("lat").GetDouble(), location.GetProperty("lng").GetDouble());
        }

        // Aynı normalizasyon mantığı: GeocodingTool ve GoogleMapsRouteOptimizationService
        private static string NormalizeAddress(string address) =>
            Regex.Replace(
                address
                    .Replace("\\", "/")
                    .Replace("NO:", "No ")
                    .Replace("MAH.", "Mah.")
                    .Replace("CAD.", "Cad.")
                    .Replace("SOK.", "Sok.")
                    .Trim(),
                @"\s{2,}", " ");
    }
}
