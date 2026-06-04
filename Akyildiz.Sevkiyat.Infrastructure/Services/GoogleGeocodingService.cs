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

        public async Task<GeocodedLocation?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
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

            if (root.GetProperty("status").GetString() != "OK") return null;

            var result = root.GetProperty("results")[0];

            var location = result.GetProperty("geometry").GetProperty("location");
            var lat = location.GetProperty("lat").GetDouble();
            var lng = location.GetProperty("lng").GetDouble();

            string? cityName     = null;
            string? districtName = null;

            if (result.TryGetProperty("address_components", out var components))
            {
                foreach (var comp in components.EnumerateArray())
                {
                    var longName = comp.GetProperty("long_name").GetString();
                    var types    = comp.GetProperty("types").EnumerateArray()
                                       .Select(t => t.GetString())
                                       .ToList();

                    // İl: administrative_area_level_1
                    if (types.Contains("administrative_area_level_1") && cityName == null)
                        cityName = longName;

                    // İlçe: administrative_area_level_2 (Türkiye'de ilçe düzeyi)
                    if (types.Contains("administrative_area_level_2") && districtName == null)
                        districtName = longName;
                }
            }

            return new GeocodedLocation(lat, lng, cityName, districtName);
        }

        public async Task<GeocodedLocation?> ReverseGeocodeAsync(double lat, double lng, CancellationToken cancellationToken = default)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var latlng = $"{lat.ToString(inv)},{lng.ToString(inv)}";
            var url = "https://maps.googleapis.com/maps/api/geocode/json" +
                      $"?latlng={Uri.EscapeDataString(latlng)}" +
                      $"&key={_apiKey}" +
                      "&language=tr&region=tr" +
                      "&result_type=administrative_area_level_1|administrative_area_level_2";

            using var response = await _http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = doc.RootElement;

            if (root.GetProperty("status").GetString() != "OK") return null;

            string? cityName     = null;
            string? districtName = null;

            foreach (var res in root.GetProperty("results").EnumerateArray())
            {
                if (!res.TryGetProperty("address_components", out var components)) continue;
                foreach (var comp in components.EnumerateArray())
                {
                    var longName = comp.GetProperty("long_name").GetString();
                    var types    = comp.GetProperty("types").EnumerateArray()
                                       .Select(t => t.GetString())
                                       .ToList();

                    if (types.Contains("administrative_area_level_1") && cityName == null)
                        cityName = longName;
                    if (types.Contains("administrative_area_level_2") && districtName == null)
                        districtName = longName;
                }
                if (cityName != null && districtName != null) break;
            }

            return new GeocodedLocation(lat, lng, cityName, districtName);
        }

        public async Task<GeocodedLocation?> PlaceSearchAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            var url = "https://maps.googleapis.com/maps/api/place/findplacefromtext/json" +
                      $"?input={Uri.EscapeDataString(query)}" +
                      "&inputtype=textquery" +
                      "&fields=geometry" +
                      $"&key={_apiKey}" +
                      "&language=tr&region=tr";

            using var response = await _http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = doc.RootElement;

            var status = root.GetProperty("status").GetString();
            if (status != "OK") return null; // ZERO_RESULTS, REQUEST_DENIED (Places kapalı) vb.

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                return null;

            var loc = candidates[0].GetProperty("geometry").GetProperty("location");
            return new GeocodedLocation(loc.GetProperty("lat").GetDouble(), loc.GetProperty("lng").GetDouble(), null, null);
        }

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
