using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.External.IssIp.Dtos;
using Akyildiz.Sevkiyat.Infrastructure.ExternalServices.IssIp;
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

        public ISSIpClient(HttpClient http, IOptions<ISSIpOptions> opt)
        {
            _http = http;
            _opt = opt.Value;

            if (!string.IsNullOrEmpty(_opt.BasicAuthUsername) && !string.IsNullOrEmpty(_opt.BasicAuthPassword))
            {
                var authValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_opt.BasicAuthUsername}:{_opt.BasicAuthPassword}"));
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
            }
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

            // EndpointPath config'ten gelir. Boşsa / varsayılır.
            var url = string.IsNullOrWhiteSpace(_opt.EndpointPath) ? "/" : _opt.EndpointPath;

            using var resp = await _http.PostAsJsonAsync(url, req, cancellationToken: ct);
            
            if (!resp.IsSuccessStatusCode)
            {
                var errorContent = await resp.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"ISS-IP API Error ({resp.StatusCode}): {errorContent}");
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            // Cloning RootElement to keep it independent of the Document (which is disposed)
            return new ISSIpEnvelope { Root = doc.RootElement.Clone() };
        }
    }
}
