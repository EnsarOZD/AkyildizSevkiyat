using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Web;

namespace Akyildiz.Sevkiyat.Infrastructure.ExternalServices.Netsis
{
    public sealed class NetsisClient : INetsisClient
    {
        private readonly HttpClient _http;
        private readonly NetsisOptions _opt;
        private readonly NetsisTokenCache _tokenCache;

        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

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

                // GET /api/v2/token — Postman collection'daki gibi raw body ile gönderilir
                // (disableBodyPruning: true — query string değil, body içinde).
                // Not: dbtype parametresi bu Netsis versiyonunda (9.0.67.0) server-side
                // unhandled exception'a neden oluyor — gönderilmiyor.
                var bodyStr = new StringBuilder();
                bodyStr.Append("grant_type=password");
                bodyStr.Append("&branchcode=").Append(HttpUtility.UrlEncode(_opt.SubeKodu));
                bodyStr.Append("&username=").Append(HttpUtility.UrlEncode(_opt.KullaniciAdi));
                bodyStr.Append("&password=").Append(HttpUtility.UrlEncode(_opt.Sifre));
                bodyStr.Append("&dbname=").Append(HttpUtility.UrlEncode(_opt.DbName));
                bodyStr.Append("&dbuser=").Append(HttpUtility.UrlEncode(_opt.DbUser));
                bodyStr.Append("&dbpassword=").Append(HttpUtility.UrlEncode(_opt.DbPassword));

                using var loginReq = new HttpRequestMessage(HttpMethod.Get, _opt.LoginPath);
                loginReq.Content = new StringContent(bodyStr.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");

                using var resp = await _http.SendAsync(loginReq, cancellationToken);

                if (!resp.IsSuccessStatusCode)
                {
                    var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException(
                        $"Netsis login başarısız ({resp.StatusCode}): {err}");
                }

                var loginResult = await resp.Content.ReadFromJsonAsync<NetsisLoginResponse>(
                    cancellationToken: cancellationToken)
                    ?? throw new InvalidOperationException("Netsis login yanıtı boş döndü.");

                if (string.IsNullOrWhiteSpace(loginResult.AccessToken))
                    throw new InvalidOperationException("Netsis login yanıtında access_token boş.");

                _tokenCache.SetToken(loginResult.AccessToken, _opt.TokenExpiryMinutes);
                return loginResult.AccessToken;
            }
            finally
            {
                _tokenCache.Lock.Release();
            }
        }

        private void SetAuthHeader(string token)
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>
        /// "000" + siparisNo, toplam 15 hane, sağdan 15 karakter alınır.
        /// </summary>
        private static string FormatFatIrsNo(string siparisNo)
        {
            var padded = ("000" + siparisNo).PadLeft(15, '0');
            return padded.Substring(padded.Length - 15);
        }

        private static string FormatDate(DateTime dt) => dt.ToString(DateFormat);
        private static string FormatDate(DateOnly d)  => d.ToDateTime(TimeOnly.MinValue).ToString(DateFormat);
        private static string Truncate(string? s, int max) =>
            string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);

        // ── API Operations ──────────────────────────────────────────────────────

        public async Task<NetsisSiparisResult> CreateSiparisAsync(
            NetsisSiparisRequest request, CancellationToken cancellationToken = default)
        {
            var token = await EnsureTokenAsync(cancellationToken);
            SetAuthHeader(token);

            var now    = DateTime.Now;
            var nowStr = FormatDate(now);
            var teslimStr = FormatDate(request.TeslimTarihi);
            var fatIrsNo  = FormatFatIrsNo(request.BelgeNo);
            var depoKodu  = request.DepoKodu ?? _opt.DepoKodu ?? "1";

            var wireRequest = new NetsisCreateItemSlipRequest
            {
                FaturaTip = 7,
                FatUst = new NetsisFatUst
                {
                    Sube_Kodu    = _opt.SubeKodu,
                    FATIRS_NO    = fatIrsNo,
                    CariKod      = request.CariKodu,
                    CARI_KOD2    = request.ProjeKodu,
                    Tarih        = nowStr,
                    Tip          = 7,
                    TIPI         = 7,
                    ENTEGRE_TRH  = nowStr,
                    KDV_DAHILMI  = false,
                    SIPARIS_TEST = teslimStr,
                    D_YEDEK10    = teslimStr,
                    FIYATTARIHI  = nowStr,
                    KOSULTARIHI  = nowStr,
                    EKACK1       = request.SiparisId,
                    EKACK2       = request.KurumKodu,
                    EKACK3       = request.TalepNo,
                    EKACK4       = request.TalepTuru,
                    EKACK6       = Truncate(request.Donem, 100),
                    EKACK7       = Truncate(request.TeslimAlacakKisiler, 100),
                    EKACK8       = Truncate(request.TeslimAlacakTelefonNumaralari, 100),
                    EKACK9       = Truncate(request.YoneticiMailAdresleri, 100),
                    EKACK16      = "0",
                },
                Kalems = request.Satirlar.Select(s => new NetsisKalem
                {
                    StokKodu      = s.StokKodu,
                    DEPO_KODU     = depoKodu,
                    STra_GCMIK    = s.Miktar,
                    STra_BF       = s.BirimFiyati,
                    STra_KDV      = s.KdvOrani,
                    STra_testar   = teslimStr,
                    Stra_KosTar   = nowStr,
                    Stra_FiyatTar = nowStr,
                }).ToList(),
            };

            using var resp = await _http.PostAsJsonAsync(_opt.SiparisPath, wireRequest, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _tokenCache.Invalidate();
                var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Netsis CreateSiparis hatası ({resp.StatusCode}): {err}");
            }

            return new NetsisSiparisResult
            {
                Basarili     = true,
                NetsisOrderNo = fatIrsNo,
            };
        }

        public async Task<NetsisPoResult> CreateSatinalmaSiparisAsync(
            NetsisPoRequest request, CancellationToken cancellationToken = default)
        {
            var token = await EnsureTokenAsync(cancellationToken);
            SetAuthHeader(token);

            var now      = DateTime.Now;
            var nowStr   = FormatDate(now);
            var fatIrsNo = FormatFatIrsNo(request.BelgeNo);
            var depoKodu = request.DepoKodu ?? _opt.DepoKodu ?? "1";
            var teslimStr = request.TeslimTarihi.HasValue
                ? FormatDate(request.TeslimTarihi.Value)
                : nowStr;

            var wireRequest = new NetsisCreateItemSlipRequest
            {
                FaturaTip = 6,
                FatUst = new NetsisFatUst
                {
                    Sube_Kodu    = _opt.SubeKodu,
                    FATIRS_NO    = fatIrsNo,
                    CariKod      = request.TedarikciKodu,
                    CARI_KOD2    = string.Empty,
                    Tarih        = FormatDate(request.SiparisDate),
                    Tip          = 6,
                    TIPI         = 7,
                    ENTEGRE_TRH  = nowStr,
                    KDV_DAHILMI  = false,
                    SIPARIS_TEST = teslimStr,
                    D_YEDEK10    = teslimStr,
                    FIYATTARIHI  = nowStr,
                    KOSULTARIHI  = nowStr,
                    // EKACK alanları yok (satınalma siparişi)
                },
                Kalems = request.Satirlar.Select(s => new NetsisKalem
                {
                    StokKodu      = s.StokKodu,
                    DEPO_KODU     = depoKodu,
                    STra_GCMIK    = s.Miktar,
                    STra_BF       = 0,
                    STra_KDV      = 0,
                    STra_testar   = teslimStr,
                    Stra_KosTar   = FormatDate(request.SiparisDate),
                    Stra_FiyatTar = FormatDate(request.SiparisDate),
                }).ToList(),
            };

            using var resp = await _http.PostAsJsonAsync(_opt.SatinalmaSiparisPath, wireRequest, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _tokenCache.Invalidate();
                var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Netsis CreateSatinalmaSiparis hatası ({resp.StatusCode}): {err}");
            }

            return new NetsisPoResult
            {
                Basarili  = true,
                NetsisPONo = fatIrsNo,
            };
        }

        public async Task<IReadOnlyList<NetsisIrsaliyeDto>> GetIrsaliyelerAsync(
            NetsisIrsaliyeQuery query, CancellationToken cancellationToken = default)
        {
            var token = await EnsureTokenAsync(cancellationToken);
            SetAuthHeader(token);

            // GET /api/v2/ItemSlips/ftSSip;{FATIRS_NO};{CariKod}
            var fatIrsNo = FormatFatIrsNo(query.SiparisNo ?? string.Empty);
            var cariKod  = HttpUtility.UrlEncode(query.CariKod ?? string.Empty);
            var url      = $"{_opt.IrsaliyePath}/ftSSip;{fatIrsNo};{cariKod}";

            using var resp = await _http.GetAsync(url, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _tokenCache.Invalidate();
                var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Netsis GetIrsaliyeler hatası ({resp.StatusCode}): {err}");
            }

            // Netsis tek bir irsaliye nesnesi döndürebilir ya da liste
            var raw = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(raw) || raw == "null")
                return Array.Empty<NetsisIrsaliyeDto>();

            if (raw.TrimStart().StartsWith('['))
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<NetsisIrsaliyeDto>>(raw)
                    as IReadOnlyList<NetsisIrsaliyeDto>
                    ?? Array.Empty<NetsisIrsaliyeDto>();
            }
            else
            {
                var single = System.Text.Json.JsonSerializer.Deserialize<NetsisIrsaliyeDto>(raw);
                return single is null ? Array.Empty<NetsisIrsaliyeDto>() : new[] { single };
            }
        }

        public async Task<IReadOnlyList<NetsisStockBalanceDto>> GetStockBalancesAsync(
            string? stokKodu = null, string? depoKodu = null, CancellationToken cancellationToken = default)
        {
            var token = await EnsureTokenAsync(cancellationToken);
            SetAuthHeader(token);

            var depo = depoKodu ?? _opt.DepoKodu ?? "1";

            // GET /api/v2/Queries?tsql=SELECT (TOP_GIRIS_MIK-TOP_CIKIS_MIK) FROM TBLSTOKPH WHERE ...
            var tsql = new StringBuilder("SELECT (TOP_GIRIS_MIK-TOP_CIKIS_MIK) FROM TBLSTOKPH");
            tsql.Append($" WHERE DEPO_KODU={depo}");
            if (!string.IsNullOrWhiteSpace(stokKodu))
                tsql.Append($" AND STOK_KODU='{stokKodu.Replace("'", "''")}'");

            var url = $"{_opt.StokBakiyePath}?tsql={HttpUtility.UrlEncode(tsql.ToString())}";

            using var resp = await _http.GetAsync(url, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _tokenCache.Invalidate();
                var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Netsis GetStockBalance hatası ({resp.StatusCode}): {err}");
            }

            var raw = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(raw) || raw == "null")
                return Array.Empty<NetsisStockBalanceDto>();

            // Response: [[123.45]] (DataTable) veya [{"Column1": 123.45}]
            decimal mevcutStok = 0;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var first = root[0];
                    if (first.ValueKind == System.Text.Json.JsonValueKind.Array && first.GetArrayLength() > 0)
                        mevcutStok = first[0].GetDecimal();
                    else if (first.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        foreach (var prop in first.EnumerateObject())
                        {
                            mevcutStok = prop.Value.GetDecimal();
                            break;
                        }
                    }
                    else if (first.ValueKind == System.Text.Json.JsonValueKind.Number)
                        mevcutStok = first.GetDecimal();
                }
                else if (root.ValueKind == System.Text.Json.JsonValueKind.Number)
                    mevcutStok = root.GetDecimal();
            }
            catch
            {
                if (decimal.TryParse(raw.Trim('[', ']', '"', ' '), out var parsed))
                    mevcutStok = parsed;
            }

            return new[]
            {
                new NetsisStockBalanceDto
                {
                    StokKodu   = stokKodu ?? string.Empty,
                    DepoKodu   = depo,
                    MevcutStok = mevcutStok,
                },
            };
        }
    }
}
