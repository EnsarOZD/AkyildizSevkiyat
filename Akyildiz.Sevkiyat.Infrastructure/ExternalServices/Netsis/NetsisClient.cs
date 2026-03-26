using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Akyildiz.Sevkiyat.Infrastructure.ExternalServices.Netsis
{
    public sealed class NetsisClient : INetsisClient
    {
        private readonly HttpClient _http;
        private readonly NetsisOptions _opt;
        private readonly NetsisTokenCache _tokenCache;

        public NetsisClient(HttpClient http, IOptions<NetsisOptions> opt, NetsisTokenCache tokenCache)
        {
            _http       = http;
            _opt        = opt.Value;
            _tokenCache = tokenCache;
        }

        // ── Token Management ────────────────────────────────────────────────────

        private async Task<string> EnsureTokenAsync(CancellationToken cancellationToken)
        {
            if (_tokenCache.Token is not null)
                return _tokenCache.Token;

            await _tokenCache.Lock.WaitAsync(cancellationToken);
            try
            {
                // Double-check after acquiring lock
                if (_tokenCache.Token is not null)
                    return _tokenCache.Token;

                // TODO: NETSIS_API — login request body field names — güncelle
                var loginPayload = new NetsisLoginRequest
                {
                    KullaniciAdi = _opt.KullaniciAdi,
                    Sifre        = _opt.Sifre,
                    FirmaKodu    = _opt.FirmaKodu,
                    SubeKodu     = _opt.SubeKodu,
                    IsletmeKodu  = _opt.IsletmeKodu,
                };

                using var resp = await _http.PostAsJsonAsync(_opt.LoginPath, loginPayload, cancellationToken);

                if (!resp.IsSuccessStatusCode)
                {
                    var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Netsis login başarısız ({resp.StatusCode}): {err}");
                }

                // TODO: NETSIS_API — login response field name (Token, AccessToken, Result, ...)
                var loginResult = await resp.Content.ReadFromJsonAsync<NetsisLoginResponse>(cancellationToken: cancellationToken)
                    ?? throw new InvalidOperationException("Netsis login yanıtı boş döndü.");

                if (string.IsNullOrWhiteSpace(loginResult.Token))
                    throw new InvalidOperationException("Netsis login yanıtında token alanı boş.");

                // ExpireDate geliyorsa kullan, gelmiyorsa config'teki dakika değerini kullan
                int expiryMinutes = _opt.TokenExpiryMinutes;
                if (loginResult.ExpireDate.HasValue)
                {
                    var remaining = (int)(loginResult.ExpireDate.Value - DateTime.UtcNow).TotalMinutes - 5;
                    if (remaining > 0) expiryMinutes = remaining;
                }

                _tokenCache.SetToken(loginResult.Token, expiryMinutes);
                return loginResult.Token;
            }
            finally
            {
                _tokenCache.Lock.Release();
            }
        }

        private void SetAuthHeader(string token)
        {
            // TODO: NETSIS_API — auth header schema (Bearer, Token, custom header?) — güncelle
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        // ── API Operations ──────────────────────────────────────────────────────

        public async Task<NetsisSiparisResult> CreateSiparisAsync(
            NetsisSiparisRequest request, CancellationToken cancellationToken = default)
        {
            // Guard: path henüz yapılandırılmamışsa anlamlı hata ver
            if (_opt.SiparisPath == "PENDING_NETSIS_API_DOCS")
                throw new InvalidOperationException(
                    "Netsis sipariş endpoint'i henüz yapılandırılmamış. " +
                    "NetsisOptions.SiparisPath değerini güncelleyin.");

            var token = await EnsureTokenAsync(cancellationToken);
            SetAuthHeader(token);

            // TODO: NETSIS_API — endpoint path, HTTP method (POST/PUT?) ve body yapısını güncelle
            using var resp = await _http.PostAsJsonAsync(_opt.SiparisPath, request, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _tokenCache.Invalidate(); // 401 ihtimaline karşı token'ı sıfırla
                var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Netsis CreateSiparis hatası ({resp.StatusCode}): {err}");
            }

            // TODO: NETSIS_API — response yapısını güncelle
            return await resp.Content.ReadFromJsonAsync<NetsisSiparisResult>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Netsis sipariş yanıtı boş döndü.");
        }

        public async Task<NetsisPoResult> CreateSatinalmaSiparisAsync(
            NetsisPoRequest request, CancellationToken cancellationToken = default)
        {
            if (_opt.SatinalmaSiparisPath == "PENDING_NETSIS_API_DOCS")
                throw new InvalidOperationException(
                    "Netsis satınalma sipariş endpoint'i henüz yapılandırılmamış. " +
                    "NetsisOptions.SatinalmaSiparisPath değerini güncelleyin.");

            var token = await EnsureTokenAsync(cancellationToken);
            SetAuthHeader(token);

            // TODO: NETSIS_API — endpoint path, HTTP method ve body yapısını güncelle
            using var resp = await _http.PostAsJsonAsync(_opt.SatinalmaSiparisPath, request, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _tokenCache.Invalidate();
                var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Netsis CreateSatinalmaSiparis hatası ({resp.StatusCode}): {err}");
            }

            // TODO: NETSIS_API — response yapısını güncelle
            return await resp.Content.ReadFromJsonAsync<NetsisPoResult>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Netsis satınalma sipariş yanıtı boş döndü.");
        }

        public async Task<IReadOnlyList<NetsisIrsaliyeDto>> GetIrsaliyelerAsync(
            NetsisIrsaliyeQuery query, CancellationToken cancellationToken = default)
        {
            if (_opt.IrsaliyePath == "PENDING_NETSIS_API_DOCS")
                throw new InvalidOperationException(
                    "Netsis irsaliye endpoint'i henüz yapılandırılmamış. " +
                    "NetsisOptions.IrsaliyePath değerini güncelleyin.");

            var token = await EnsureTokenAsync(cancellationToken);
            SetAuthHeader(token);

            // TODO: NETSIS_API — GET mi POST mu? query string mi, body mi? — güncelle
            using var resp = await _http.PostAsJsonAsync(_opt.IrsaliyePath, query, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _tokenCache.Invalidate();
                var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Netsis GetIrsaliyeler hatası ({resp.StatusCode}): {err}");
            }

            // TODO: NETSIS_API — response wrapper var mı? — güncelle
            return await resp.Content.ReadFromJsonAsync<List<NetsisIrsaliyeDto>>(cancellationToken: cancellationToken)
                ?? new List<NetsisIrsaliyeDto>();
        }

        public async Task<IReadOnlyList<NetsisStockBalanceDto>> GetStockBalancesAsync(
            string? stokKodu = null, string? depoKodu = null, CancellationToken cancellationToken = default)
        {
            if (_opt.StokBakiyePath == "PENDING_NETSIS_API_DOCS")
                throw new InvalidOperationException(
                    "Netsis stok bakiye endpoint'i henüz yapılandırılmamış. " +
                    "NetsisOptions.StokBakiyePath değerini güncelleyin.");

            var token = await EnsureTokenAsync(cancellationToken);
            SetAuthHeader(token);

            var query = new NetsisStockBalanceQuery
            {
                StokKodu = stokKodu,
                DepoKodu = depoKodu ?? _opt.DepoKodu,
            };

            // TODO: NETSIS_API — GET mi POST mu? query string mi, body mi? — güncelle
            using var resp = await _http.PostAsJsonAsync(_opt.StokBakiyePath, query, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _tokenCache.Invalidate();
                var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Netsis GetStockBalance hatası ({resp.StatusCode}): {err}");
            }

            // TODO: NETSIS_API — response wrapper var mı? (örn. { "data": [...] }) — güncelle
            return await resp.Content.ReadFromJsonAsync<List<NetsisStockBalanceDto>>(cancellationToken: cancellationToken)
                ?? new List<NetsisStockBalanceDto>();
        }
    }
}
