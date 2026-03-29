using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

// ─────────────────────────────────────────────────────────────────────────────
//  Akyildiz Sevkiyat — Tek Seferlik Toplu Geocoding Aracı
//
//  Kullanım:
//    dotnet run -- --conn "Server=...;Database=..." --key "AIzaSy..."
//
//  Veya ortam değişkenleri:
//    GEOCODING_CONN  = connection string
//    GEOCODING_KEY   = Google Maps API key
//
//  appsettings.json yoksa yukarıdaki değerleri kullan.
//  İdempotent: sadece Latitude IS NULL AND Address IS NOT NULL olanları işler.
// ─────────────────────────────────────────────────────────────────────────────

var sw = Stopwatch.StartNew();

// ── 1. Config ─────────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var connectionString = config["conn"]
    ?? config["ConnectionStrings:SevkiyatConnection"]
    ?? config["GEOCODING_CONN"]
    ?? throw new Exception("Connection string bulunamadı. --conn veya GEOCODING_CONN ortam değişkeni kullanın.");

var apiKey = config["key"]
    ?? config["GoogleMaps:ApiKey"]
    ?? config["GEOCODING_KEY"]
    ?? throw new Exception("API key bulunamadı. --key veya GEOCODING_KEY ortam değişkeni kullanın.");

// ── 2. Projeleri çek ─────────────────────────────────────────────────────────
Console.WriteLine("Projeler veritabanından yükleniyor...");

var projects = new List<(int Id, string Code, string Address)>();

await using (var conn = new SqlConnection(connectionString))
{
    await conn.OpenAsync();
    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT Id, Code, Address
        FROM Projects
        WHERE IsActive = 1
          AND Latitude IS NULL
          AND Address IS NOT NULL
          AND LTRIM(RTRIM(Address)) <> ''
        ORDER BY Id";

    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        projects.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
    }
}

Console.WriteLine($"{projects.Count} proje geocoding bekliyor.");
if (projects.Count == 0)
{
    Console.WriteLine("İşlenecek proje yok. Çıkılıyor.");
    return;
}

// ── 3. Geocoding ──────────────────────────────────────────────────────────────
var successCount = 0;
var failedProjects = new List<(string Code, string Address, string Reason)>();
var processed = 0;

using var http = new HttpClient();
http.Timeout = TimeSpan.FromSeconds(15);

await using var updateConn = new SqlConnection(connectionString);
await updateConn.OpenAsync();

foreach (var (id, code, rawAddress) in projects)
{
    processed++;
    var normalized = NormalizeAddress(rawAddress);

    // Rate limit: her istekten önce 100ms bekle (≤10 req/s)
    await Task.Delay(100);

    int retryCount = 0;
retry:
    try
    {
        var url = $"https://maps.googleapis.com/maps/api/geocode/json" +
                  $"?address={Uri.EscapeDataString(normalized)}" +
                  $"&key={apiKey}" +
                  $"&language=tr" +
                  $"&region=tr";

        using var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var status = root.GetProperty("status").GetString();

        switch (status)
        {
            case "OK":
            {
                var location = root
                    .GetProperty("results")[0]
                    .GetProperty("geometry")
                    .GetProperty("location");
                var lat = location.GetProperty("lat").GetDouble();
                var lng = location.GetProperty("lng").GetDouble();

                await UpdateProjectCoordinates(updateConn, id, lat, lng);
                successCount++;

                if (processed % 100 == 0 || processed == projects.Count)
                    Console.WriteLine($"  {processed}/{projects.Count} tamamlandı — son: {code} ({lat:F6}, {lng:F6})");
                break;
            }
            case "ZERO_RESULTS":
                failedProjects.Add((code, rawAddress, "ZERO_RESULTS — adres bulunamadı"));
                break;

            case "OVER_QUERY_LIMIT":
                if (retryCount < 3)
                {
                    retryCount++;
                    Console.WriteLine($"  OVER_QUERY_LIMIT — 2s bekleniyor (deneme {retryCount}/3)...");
                    await Task.Delay(2000);
                    goto retry;
                }
                failedProjects.Add((code, rawAddress, "OVER_QUERY_LIMIT — limit aşıldı"));
                break;

            case "REQUEST_DENIED":
                Console.WriteLine($"\n[HATA] REQUEST_DENIED: API key geçersiz veya Geocoding API etkin değil.");
                Console.WriteLine("Google Cloud Console > APIs & Services > Geocoding API'yi etkinleştirin.");
                Console.WriteLine($"Kullanılan key: {apiKey[..10]}...");
                return;

            default:
                failedProjects.Add((code, rawAddress, $"status={status}"));
                break;
        }
    }
    catch (Exception ex)
    {
        if (retryCount < 2)
        {
            retryCount++;
            await Task.Delay(1000);
            goto retry;
        }
        failedProjects.Add((code, rawAddress, $"Hata: {ex.Message}"));
        Console.Error.WriteLine($"  [{code}] Hata: {ex.Message}");
    }
}

// ── 4. Sonuç raporu ───────────────────────────────────────────────────────────
sw.Stop();
Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine($"  Geocoding tamamlandı — {sw.Elapsed.TotalSeconds:F1}s");
Console.WriteLine($"  ✓ Başarılı  : {successCount} proje güncellendi");
Console.WriteLine($"  ✗ Başarısız : {failedProjects.Count} proje");

if (failedProjects.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("  Başarısız projeler:");
    foreach (var (failCode, failAddr, reason) in failedProjects)
        Console.WriteLine($"    [{failCode}] \"{failAddr}\" → {reason}");
}

Console.WriteLine("═══════════════════════════════════════════════════════════");

// ── Yardımcı fonksiyonlar ────────────────────────────────────────────────────

static async Task UpdateProjectCoordinates(SqlConnection conn, int projectId, double lat, double lng)
{
    var cmd = conn.CreateCommand();
    cmd.CommandText = "UPDATE Projects SET Latitude = @lat, Longitude = @lng WHERE Id = @id AND Latitude IS NULL";
    cmd.Parameters.AddWithValue("@lat", lat);
    cmd.Parameters.AddWithValue("@lng", lng);
    cmd.Parameters.AddWithValue("@id", projectId);
    await cmd.ExecuteNonQueryAsync();
}

static string NormalizeAddress(string address)
{
    // Aynı normalizasyon: GoogleMapsRouteOptimizationService.NormalizeAddress
    return Regex.Replace(
        address
            .Replace("\\", "/")
            .Replace("NO:", "No ")
            .Replace("MAH.", "Mah.")
            .Replace("CAD.", "Cad.")
            .Replace("SOK.", "Sok.")
            .Replace("   ", " ")
            .Replace("  ", " ")
            .Trim(),
        @"\s{2,}", " ");
}
