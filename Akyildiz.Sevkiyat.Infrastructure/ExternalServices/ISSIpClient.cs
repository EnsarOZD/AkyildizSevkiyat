using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.External.IssIp.Dtos;
using Akyildiz.Sevkiyat.Infrastructure.ExternalServices.IssIp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Infrastructure.ExternalServices
{
    public sealed class ISSIpClient : IISSIpClient
    {
        private readonly HttpClient _http;
        private readonly ISSIpOptions _opt;
        private readonly ILogger<ISSIpClient> _logger;

        public ISSIpClient(HttpClient http, IOptions<ISSIpOptions> opt, ILogger<ISSIpClient> logger)
        {
            _http = http;
            _opt = opt.Value;
            _logger = logger;

            var authValue = Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($"{_opt.BasicAuthUsername}:{_opt.BasicAuthPassword}"));
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
        }

        public Task<ISSIpEnvelope> GetSiparisListesiAsync(DateTime start, DateTime end, CancellationToken ct)
            => PostAsync("getTedarikciSiparisListesi", new GetSiparisListesiParams
            {
                KullaniciAdi = _opt.KullaniciAdi,
                Sifre = _opt.Sifre,
                BaslangicTarihi = start.ToString("yyyy-MM-dd"),
                BitisTarihi = end.ToString("yyyy-MM-dd"),
            }, ct);

        public Task<ISSIpEnvelope> GetSiparisAsync(string siparisNo, CancellationToken ct)
            => PostAsync("getTedarikciSiparis", new GetSiparisParams
            {
                KullaniciAdi = _opt.KullaniciAdi,
                Sifre = _opt.Sifre,
                SiparisNo = siparisNo
            }, ct);

        public Task<ISSIpEnvelope> GetProjeAsync(string? projeKodu, CancellationToken ct)
            => PostAsync("getTedarikciProje", new GetProjeParams
            {
                KullaniciAdi = _opt.KullaniciAdi,
                Sifre = _opt.Sifre,
                ProjeKodu = projeKodu
            }, ct);

        public Task<ISSIpEnvelope> GetMalzemeAsync(string? malzemeKodu, CancellationToken ct)
            => PostAsync("getTedarikciMalzeme", new GetMalzemeParams
            {
                KullaniciAdi = _opt.KullaniciAdi,
                Sifre = _opt.Sifre,
                MalzemeKodu = malzemeKodu
            }, ct);

        private async Task<ISSIpEnvelope> PostAsync<TParams>(string methodName, TParams mtdParams, CancellationToken ct)
        {
            var req = new ISSIpRequest<TParams> { MethodName = methodName, MtdParams = mtdParams };

            var url = string.IsNullOrWhiteSpace(_opt.EndpointPath) ? "/" : _opt.EndpointPath;

            _logger.LogDebug("ISS-IP → {Method} | POST {BaseUrl}{Path}", methodName, _opt.BaseUrl, url);

            using var resp = await _http.PostAsJsonAsync(url, req, cancellationToken: ct);

            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "(none)";
            var body = await resp.Content.ReadAsStringAsync(ct);
            var preview = body.Length > 500 ? body[..500] : body;

            _logger.LogDebug(
                "ISS-IP ← {Method} | Status={Status} ContentType={ContentType} BodyPreview={Preview}",
                methodName, (int)resp.StatusCode, contentType, preview);

            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"ISS-IP [{methodName}] HTTP {(int)resp.StatusCode}: {preview}");

            // Yeni endpoint HTML döndürebilir (auth sayfası, yanlış path, vb.)
            // JSON olmayan response'u erken yakala ve diagnostik bilgiyle fırlat.
            if (!IsJson(contentType, body))
                throw new InvalidOperationException(
                    $"ISS-IP [{methodName}] JSON beklendi ama '{contentType}' döndü. " +
                    $"BaseUrl='{_opt.BaseUrl}' Path='{url}'. " +
                    $"Response (ilk 500 karakter): {preview}");

            var doc = JsonDocument.Parse(body);
            return new ISSIpEnvelope { Root = doc.RootElement.Clone() };
        }

        private static bool IsJson(string contentType, string body)
        {
            if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                return true;

            // Content-Type güvenilmez olabilir; body'nin gerçekten JSON başladığını kontrol et.
            var trimmed = body.TrimStart();
            return trimmed.StartsWith('{') || trimmed.StartsWith('[');
        }
    }
}
