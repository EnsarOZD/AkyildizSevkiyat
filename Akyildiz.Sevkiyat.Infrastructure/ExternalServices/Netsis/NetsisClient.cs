using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Akyildiz.Sevkiyat.Infrastructure.ExternalServices.Netsis
{
    public sealed class NetsisClient : INetsisClient
    {
        private readonly HttpClient _http;
        private readonly NetsisOptions _opt;
        private readonly NetsisTokenCache _tokenCache;
        private readonly ILogger<NetsisClient> _logger;

        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";


        public NetsisClient(HttpClient http, IOptions<NetsisOptions> opt, NetsisTokenCache tokenCache, ILogger<NetsisClient> logger)
        {
            _http       = http;
            _opt        = opt.Value;
            _tokenCache = tokenCache;
            _logger     = logger;
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

            var rawResp = await resp.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "CreateSiparis [{FatIrsNo}] HTTP {Status} yanıt: {Raw}",
                fatIrsNo, (int)resp.StatusCode,
                rawResp.Length > 600 ? rawResp[..600] + "…" : rawResp);

            if (!resp.IsSuccessStatusCode)
            {
                _tokenCache.Invalidate();
                throw new HttpRequestException(
                    $"Netsis CreateSiparis hatası ({resp.StatusCode}): {rawResp}");
            }

            // Boş ya da null yanıt → Netsis siparişi oluşturmadı, hata fırlat
            if (string.IsNullOrWhiteSpace(rawResp) || rawResp.Trim() == "null")
                throw new HttpRequestException(
                    $"Netsis CreateSiparis: boş yanıt alındı — sipariş kaydedilmedi. Belge No: {fatIrsNo}");

            // Netsis her zaman HTTP 200 döndürür; başarı IsSuccessful alanındadır
            NetsisApiResponse? apiResp = null;
            try { apiResp = System.Text.Json.JsonSerializer.Deserialize<NetsisApiResponse>(rawResp); } catch { }

            // Yanıt parse edilemedi (JSON değil → HTML hata sayfası olabilir)
            if (apiResp == null)
                throw new HttpRequestException(
                    $"Netsis CreateSiparis: yanıt JSON olarak okunamadı. Belge No: {fatIrsNo}. Ham yanıt: " +
                    (rawResp.Length > 300 ? rawResp[..300] : rawResp));

            if (apiResp is { IsSuccessful: false })
            {
                // Sipariş Netsis'te zaten mevcutsa (önceki aktarımda DB kaydı başarısız olmuş olabilir),
                // hata fırlatmak yerine başarılı say — belge no zaten biliniyor.
                var errorDesc = apiResp.ErrorDesc ?? string.Empty;
                // Sadece mesaj içeriğine göre "zaten mevcut" tespiti yapılır.
                // ErrorCode == "101" Netsis'in genel hata kodu olup stok kodu hatası,
                // geçersiz kalem bilgisi vb. durumlar için de döner — tek başına güvenilir değil.
                var isAlreadyExists =
                    (errorDesc.Contains("kaydedilmiş", StringComparison.OrdinalIgnoreCase) && errorDesc.Contains("nolu evrak", StringComparison.OrdinalIgnoreCase)) ||
                    (errorDesc.Contains("zaten",       StringComparison.OrdinalIgnoreCase) && errorDesc.Contains("mevcut",     StringComparison.OrdinalIgnoreCase)) ||
                    (errorDesc.Contains("önceden",     StringComparison.OrdinalIgnoreCase) && errorDesc.Contains("kayıt",      StringComparison.OrdinalIgnoreCase)) ||
                    errorDesc.Contains("duplicate",    StringComparison.OrdinalIgnoreCase);

                if (isAlreadyExists)
                {
                    // Netsis hata mesajından gerçek FATIRS_NO'yu çek.
                    // Örnek: "000202604110281 Nolu Evrak Daha önceden kaydedilmiş..."
                    // Bizim hesapladığımız fatIrsNo farklı olabilir (Netsis kendi sıra numarasını atamış olabilir).
                    var noMatch = Regex.Match(errorDesc, @"\b(\d{15})\b");
                    var realFatIrsNo = noMatch.Success ? noMatch.Groups[1].Value : fatIrsNo;

                    _logger.LogWarning(
                        "CreateSiparis: Netsis '{FatIrsNo}' zaten mevcut — aktarılmış kabul ediliyor (gerçek no: {RealNo}). Hata: [{Code}] {Desc}",
                        fatIrsNo, realFatIrsNo, apiResp.ErrorCode, apiResp.ErrorDesc);

                    return new NetsisSiparisResult
                    {
                        Basarili      = true,
                        NetsisOrderNo = realFatIrsNo,
                        Mesaj         = $"Sipariş Netsis'te zaten mevcuttu, durum güncellendi. [{apiResp.ErrorCode}] {apiResp.ErrorDesc}",
                    };
                }

                throw new HttpRequestException(
                    $"Netsis CreateSiparis hatası: [{apiResp.ErrorCode}] {apiResp.ErrorDesc}");
            }

            return new NetsisSiparisResult
            {
                Basarili      = true,
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
                    STra_KDV      = s.KdvOrani,
                    STra_testar   = teslimStr,
                    Stra_KosTar   = FormatDate(request.SiparisDate),
                    Stra_FiyatTar = FormatDate(request.SiparisDate),
                }).ToList(),
            };

            using var resp = await _http.PostAsJsonAsync(_opt.SatinalmaSiparisPath, wireRequest, cancellationToken);

            var rawResp = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _tokenCache.Invalidate();
                throw new HttpRequestException(
                    $"Netsis CreateSatinalmaSiparis hatası ({resp.StatusCode}): {rawResp}");
            }

            NetsisApiResponse? apiResp = null;
            try { apiResp = System.Text.Json.JsonSerializer.Deserialize<NetsisApiResponse>(rawResp); } catch { }

            if (apiResp is { IsSuccessful: false })
            {
                throw new HttpRequestException(
                    $"Netsis CreateSatinalmaSiparis hatası: [{apiResp.ErrorCode}] {apiResp.ErrorDesc}");
            }

            return new NetsisPoResult
            {
                Basarili   = true,
                NetsisPONo = fatIrsNo,
            };
        }

        public async Task<IReadOnlyList<NetsisIrsaliyeDto>> GetIrsaliyelerAsync(
            NetsisIrsaliyeQuery query, CancellationToken cancellationToken = default)
        {
            var token = await EnsureTokenAsync(cancellationToken);
            SetAuthHeader(token);

            // İrsaliye FATIRS_NO'su (AKI...) sipariş FATIRS_NO'sundan farklı.
            // Strateji:
            //   1. SQL ile TBLSIPUST'tan SIPARIS_NO = siparisNo olan irsaliye kaydını bul → gerçek FATIRS_NO'yu al.
            //   2. Bulunan FATIRS_NO ile ftSIrs;{irsaliyeNo}; çağır.
            var siparisNo = FormatFatIrsNo(query.SiparisNo ?? string.Empty);

            var irsaliyeNo = await FindIrsaliyeNoViaSqlAsync(siparisNo, query.CariKod, cancellationToken);

            if (irsaliyeNo == null)
            {
                _logger.LogInformation("GetIrsaliyeler: {SiparisNo} için henüz irsaliye kesilmemiş.", siparisNo);
                return Array.Empty<NetsisIrsaliyeDto>();
            }

            _logger.LogInformation("GetIrsaliyeler: {SiparisNo} → IrsaliyeNo={IrsaliyeNo}", siparisNo, irsaliyeNo);

            var (result, _) = await FetchIrsaliyeByNoAsync(irsaliyeNo, cancellationToken);
            if (result != null)
                return result;

            // FetchIrsaliyeByNoAsync yanıtı parse edemedi ama TBLSTHAR'dan numarayı zaten biliyoruz.
            // Tarih bilgisi olmadan yalnızca numarayla kaydediyoruz.
            _logger.LogInformation(
                "GetIrsaliyeler: {IrsaliyeNo} detay yanıtı parse edilemedi; numara SQL'den alınarak kaydediliyor.",
                irsaliyeNo);
            return new List<NetsisIrsaliyeDto> { new NetsisIrsaliyeDto { IrsaliyeNo = irsaliyeNo } };
        }

        /// <summary>
        /// Netsis'te verilen sipariş numarasına bağlı satış irsaliyesinin FISNO'sunu döndürür.
        ///
        /// Netsis şeması (Cenk Terim / Netsis Destek tarafından doğrulandı):
        ///   TBLSTHAR.STHAR_SIPNUM  = sipariş numarası
        ///   TBLSTHAR.FISNO         = irsaliye/fatura belge numarası
        ///   TBLSTHAR.STHAR_FTIRSIP = '3' → satış irsaliyesi, 'A' → faturalanmış satış irsaliyesi
        /// </summary>
        private async Task<string?> FindIrsaliyeNoViaSqlAsync(
            string siparisNo, string? cariKod, CancellationToken ct)
        {
            var escaped = siparisNo.Replace("'", "''");

            var tsql = $"SELECT TOP 1 FISNO FROM TBLSTHAR " +
                       $"WHERE STHAR_FTIRSIP IN ('3','A') AND STHAR_SIPNUM='{escaped}' " +
                       $"ORDER BY FISNO DESC";

            var found = await TrySqlFirstStringAsync(tsql, "FISNO", "TBLSTHAR", ct);

            if (found == "__notfound__")
            {
                _logger.LogInformation(
                    "FindIrsaliyeNo: {SiparisNo} için irsaliye henüz kesilmemiş (TBLSTHAR'da kayıt yok).",
                    siparisNo);
                return null;
            }

            if (found != null)
                return found;

            // SQL hatası — diagnostic logla
            _logger.LogWarning(
                "FindIrsaliyeNo: TBLSTHAR sorgusu başarısız, sipariş={SiparisNo}", siparisNo);
            await LogAvailableIrsaliyeTablesAsync(ct);
            return null;
        }

        /// <summary>
        /// Verilen SQL'i çalıştırır; başarılıysa ilk satırdan <paramref name="colName"/> değerini döndürür.
        /// • Kayıt yoksa    → "__notfound__" (tablo/kolon geçerli ama bu sipariş için irsaliye yok)
        /// • SQL hatası     → null (kolon/tablo adı yanlış, sıradaki stratejiyi dene)
        /// • Değer bulundu  → değer
        /// </summary>
        private async Task<string?> TrySqlFirstStringAsync(
            string tsql, string colName, string label, CancellationToken ct)
        {
            var url = $"{_opt.StokBakiyePath}?tsql={HttpUtility.UrlEncode(tsql)}";
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return null;

                var body = await resp.Content.ReadAsStringAsync(ct);
                var queryResp = System.Text.Json.JsonSerializer.Deserialize<NetsisQueryResponse>(body);

                if (queryResp is not { IsSuccessful: true })
                {
                    _logger.LogDebug("FindIrsaliyeNo [{Label}]: hata [{Code}] {Desc}",
                        label, queryResp?.ErrorCode, queryResp?.ErrorDesc);
                    return null;  // SQL hatası → sıradaki strateji
                }

                if (queryResp.Data.Count == 0)
                {
                    _logger.LogInformation("FindIrsaliyeNo [{Label}]: SQL geçerli ama irsaliye kaydı yok.", label);
                    return "__notfound__";
                }

                var row = queryResp.Data[0];
                foreach (var kv in row)
                {
                    if (kv.Key.Equals(colName, StringComparison.OrdinalIgnoreCase) &&
                        kv.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var val = kv.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            _logger.LogInformation("FindIrsaliyeNo [{Label}]: irsaliye bulundu → {Val}", label, val.Trim());
                            return val.Trim();
                        }
                    }
                }
                return "__notfound__";
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "FindIrsaliyeNo [{Label}] exception.", label);
                return null;
            }
        }

        /// <summary>
        /// Hiçbir strateji çalışmadığında INFORMATION_SCHEMA üzerinden tablo kolonlarını loglara döker.
        /// Geliştirici için diagnostic amaçlı — doğru kolon adını bulmaya yarar.
        /// </summary>
        private async Task LogAvailableIrsaliyeTablesAsync(CancellationToken ct)
        {
            _logger.LogWarning("FindIrsaliyeNo: Hiçbir SQL stratejisi çalışmadı — DIAGNOSTIC başlıyor.");
            try
            {
                var queries = new[]
                {
                    // TBLSTHAR kolonları — Netsis destek tarafından önerilen tablo
                    ("TBLSTHAR SIP/FIS kolonlar",
                     "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
                     "WHERE TABLE_NAME='TBLSTHAR' AND " +
                     "(COLUMN_NAME LIKE '%SIP%' OR COLUMN_NAME LIKE '%FIS%' OR COLUMN_NAME='STHAR_FTIRSIP') " +
                     "ORDER BY ORDINAL_POSITION"),
                };

                foreach (var (label, tsql) in queries)
                {
                    var url = $"{_opt.StokBakiyePath}?tsql={HttpUtility.UrlEncode(tsql)}";
                    using var r = await _http.GetAsync(url, ct);
                    var body = await r.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("DIAGNOSTIC [{Label}]: {Body}",
                        label, body.Length > 5000 ? body[..5000] + "..." : body);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DIAGNOSTIC sırasında exception.");
            }
        }

        /// <summary>
        /// ftSIrs;{fatIrsNo}; çağırır.
        /// Başarılıysa parse edilmiş irsaliye listesini döndürür.
        /// IsSuccessful:false ise (null, errorDesc) döndürür; HTTP hatası fırlatır.
        /// </summary>
        private async Task<(List<NetsisIrsaliyeDto>? Result, string? ErrorDesc)> FetchIrsaliyeByNoAsync(
            string fatIrsNo, CancellationToken ct)
        {
            var url = $"{_opt.IrsaliyePath}/ftSIrs;{fatIrsNo};";
            _logger.LogInformation("FetchIrsaliye → {Url}", url);

            using var resp = await _http.GetAsync(url, ct);

            if (!resp.IsSuccessStatusCode)
            {
                _tokenCache.Invalidate();
                var err = await resp.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"Netsis GetIrsaliyeler hatası ({resp.StatusCode}): {err}");
            }

            var raw = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("FetchIrsaliye yanıt ({No}): {Raw}",
                fatIrsNo, raw.Length > 800 ? raw[..800] + "..." : raw);

            if (string.IsNullOrWhiteSpace(raw) || raw == "null")
                return (null, null);

            // Netsis irsaliye yanıtı: { IsSuccessful, Data: { FatUst: { FATIRS_NO, Tarih } } }
            var irsResp = System.Text.Json.JsonSerializer.Deserialize<NetsisIrsaliyeGetResponse>(raw);

            if (irsResp is null)
                return (null, "Yanıt parse edilemedi.");

            if (!irsResp.IsSuccessful)
            {
                _logger.LogInformation(
                    "FetchIrsaliye ({No}): IsSuccessful:false [{Code}] {Desc}",
                    fatIrsNo, irsResp.ErrorCode, irsResp.ErrorDesc);
                return (null, irsResp.ErrorDesc);
            }

            var fatUst = irsResp.Data?.FatUst;
            if (fatUst is null || string.IsNullOrWhiteSpace(fatUst.FatIrsNo))
            {
                _logger.LogInformation("FetchIrsaliye ({No}): Data.FatUst boş veya FATIRS_NO yok.", fatIrsNo);
                return (null, null);
            }

            var dto = new NetsisIrsaliyeDto
            {
                IrsaliyeNo = fatUst.FatIrsNo.Trim(),
                TarihStr   = fatUst.Tarih,
            };
            return (new List<NetsisIrsaliyeDto> { dto }, null);
        }

        /// <summary>
        /// GET /api/v2/ItemSlips/ftSSip;{FATIRS_NO}; ile tek bir siparişin Netsis'te var olup olmadığını kontrol eder.
        /// 401 alınırsa token yenilenerek bir kez daha denenir.
        /// </summary>
        private async Task<(bool Exists, string RawBody)> SiparisExistsInNetsisAsync(string fatIrsNo, CancellationToken ct)
        {
            var url = $"{_opt.IrsaliyePath}/ftSSip;{fatIrsNo};";
            try
            {
                using var resp = await _http.GetAsync(url, ct);

                // Token süresi dolmuşsa yenile ve bir kez daha dene
                if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("SiparisExists [{FatIrsNo}]: 401 — token yenileniyor.", fatIrsNo);
                    _tokenCache.Invalidate();
                    var newToken = await EnsureTokenAsync(ct);
                    SetAuthHeader(newToken);

                    using var retryResp = await _http.GetAsync(url, ct);
                    return await ParseSiparisExistsResponseAsync(fatIrsNo, retryResp, ct);
                }

                return await ParseSiparisExistsResponseAsync(fatIrsNo, resp, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("SiparisExists [{FatIrsNo}]: exception — {Msg}", fatIrsNo, ex.Message);
                return (false, $"Exception: {ex.Message}");
            }
        }

        private static async Task<(bool Exists, string RawBody)> ParseSiparisExistsResponseAsync(
            string fatIrsNo, HttpResponseMessage resp, CancellationToken ct)
        {
            if (!resp.IsSuccessStatusCode)
                return (false, $"HTTP {(int)resp.StatusCode}");

            var body = await resp.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(body) || body == "null") return (false, body ?? "null");

            var trimmed = body.TrimStart();

            // {"IsSuccessful":false,...} → sipariş yok veya hata
            if (trimmed.StartsWith('{') && trimmed.Contains("\"IsSuccessful\""))
            {
                var apiResp = System.Text.Json.JsonSerializer.Deserialize<NetsisApiResponse>(body);
                return (apiResp?.IsSuccessful == true, body.Length > 300 ? body[..300] : body);
            }

            // Boş dizi [] → sipariş yok
            if (trimmed.StartsWith('['))
            {
                var nonEmpty = trimmed.Length > 2 && trimmed.TrimEnd() != "[]";
                return (nonEmpty, body.Length > 300 ? body[..300] : body);
            }

            // Nesne dönmüşse → sipariş var
            return (trimmed.StartsWith('{'), body.Length > 300 ? body[..300] : body);
        }

        public async Task<(IReadOnlySet<string> Found, string? Error)> CheckOrdersExistInNetsisAsync(
            IEnumerable<string> rawOrderNumbers,
            CancellationToken cancellationToken = default)
        {
            var orderList = rawOrderNumbers.Distinct().ToList();
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!orderList.Any()) return (found, null);

            try
            {
                var token = await EnsureTokenAsync(cancellationToken);
                SetAuthHeader(token);
            }
            catch (TaskCanceledException)
            {
                return (found, $"Netsis oturum isteği zaman aşımına uğradı ({_opt.TimeoutSeconds}s). Netsis sunucusu yanıt vermedi.");
            }
            catch (Exception ex)
            {
                return (found, $"Netsis oturum açılamadı: {ex.Message}");
            }

            // İlk örnek URL'yi logla — format doğrulaması için
            if (orderList.Count > 0)
                _logger.LogInformation(
                    "CheckOrdersExistInNetsis: İlk örnek → rawNo={Raw} fatIrsNo={Fat} url={Url}",
                    orderList[0], FormatFatIrsNo(orderList[0]),
                    $"{_opt.IrsaliyePath}/ftSSip;{FormatFatIrsNo(orderList[0])};");

            // 10'lu paralel gruplar — Netsis'i aşırı yüklememek için
            const int Concurrency = 10;
            using var semaphore = new SemaphoreSlim(Concurrency, Concurrency);
            var foundLock = new object();

            var sampleLogCount = 0;
            var tasks = orderList.Select(async rawNo =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var fatIrsNo = FormatFatIrsNo(rawNo);
                    var (exists, rawBody) = await SiparisExistsInNetsisAsync(fatIrsNo, cancellationToken);

                    // İlk 3 sorgunun yanıtını bilgi seviyesinde logla — format doğrulaması için
                    if (Interlocked.Increment(ref sampleLogCount) <= 3)
                        _logger.LogInformation(
                            "CheckOrdersExistInNetsis örnek [{N}]: rawNo={Raw} fatIrsNo={Fat} → exists={Exists} body={Body}",
                            sampleLogCount, rawNo, fatIrsNo, exists, rawBody);

                    if (exists)
                        lock (foundLock) found.Add(rawNo);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "CheckOrdersExistInNetsis: {Checked} sorgulandı, {Found} Netsis'te bulundu.",
                orderList.Count, found.Count);

            return (found, null);
        }

        public async Task<(IReadOnlySet<string> Missing, IReadOnlySet<string> Foreign, string? Error)> CheckShipmentOrdersMissingAsync(
            IEnumerable<(string NetsisOrderNo, string ShipmentId)> shipments,
            CancellationToken cancellationToken = default)
        {
            var shipmentList = shipments.ToList();
            var missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var foreign = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (shipmentList.Count == 0) return (missing, foreign, null);

            try
            {
                var token = await EnsureTokenAsync(cancellationToken);
                SetAuthHeader(token);
            }
            catch (Exception ex)
            {
                return (missing, foreign, $"Netsis oturum açılamadı: {ex.Message}");
            }

            const int Concurrency = 10;
            using var semaphore = new SemaphoreSlim(Concurrency, Concurrency);
            var resultLock = new object();

            var tasks = shipmentList.Select(async s =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var fatIrsNo = FormatFatIrsNo(s.NetsisOrderNo);
                    NetsisSiparisGetDto? detail;
                    try
                    {
                        detail = await TryGetSiparisAsync(fatIrsNo, cancellationToken);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch
                    {
                        // Ağ/parse hatası → durum belirlenemiyor, "missing" sayma; atlıyoruz.
                        return;
                    }

                    if (detail == null)
                    {
                        // Netsis'te bulunamadı → silinmiş
                        lock (resultLock) missing.Add(s.NetsisOrderNo);
                    }
                    else
                    {
                        var ekack1 = detail.GetEkack1();
                        if (!string.IsNullOrWhiteSpace(ekack1) && ekack1 != s.ShipmentId)
                        {
                            // Sipariş Netsis'te var ama başka bir sistem tarafından oluşturulmuş
                            _logger.LogWarning(
                                "CheckShipmentOrders: {FatIrsNo} Netsis'te mevcut ancak EKACK1={Ekack1} " +
                                "bizim ShipmentId={ShipmentId} ile eşleşmiyor — yabancı aktarım.",
                                fatIrsNo, ekack1, s.ShipmentId);
                            lock (resultLock) foreign.Add(s.NetsisOrderNo);
                        }
                        // else: bulunan ve EKACK1 eşleşiyor (veya EKACK1 dönmüyorsa bilinmiyor) → ok
                    }
                }
                finally { semaphore.Release(); }
            });

            await Task.WhenAll(tasks);
            return (missing, foreign, null);
        }

        /// <summary>
        /// GET /api/v2/ItemSlips/ftSSip;{fatIrsNo}; ile sipariş detayını döndürür.
        /// Bulunamazsa null döner.
        /// </summary>
        private async Task<NetsisSiparisGetDto?> TryGetSiparisAsync(string fatIrsNo, CancellationToken ct)
        {
            var url = $"{_opt.IrsaliyePath}/ftSSip;{fatIrsNo};";
            try
            {
                using var resp = await _http.GetAsync(url, ct);

                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogInformation("TryGetSiparis ({FatIrsNo}) HTTP {Status}: {Body}",
                    fatIrsNo, (int)resp.StatusCode,
                    body.Length > 400 ? body[..400] + "..." : body);

                if (!resp.IsSuccessStatusCode) return null;

                if (string.IsNullOrWhiteSpace(body) || body == "null") return null;

                var trimmed = body.TrimStart();

                if (trimmed.StartsWith('{') && trimmed.Contains("\"IsSuccessful\""))
                {
                    var apiResp = System.Text.Json.JsonSerializer.Deserialize<NetsisApiResponse>(body);
                    if (apiResp?.IsSuccessful == false) return null;
                }

                if (trimmed.StartsWith('['))
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<NetsisSiparisGetDto>>(body);
                    return list?.FirstOrDefault();
                }

                if (trimmed.StartsWith('{'))
                    return System.Text.Json.JsonSerializer.Deserialize<NetsisSiparisGetDto>(body);

                return null;
            }
            catch (OperationCanceledException)
            {
                // İptal — "bulunamadı" değil, bilinmiyor. Rethrow ile NetsisTransferredAt sıfırlanmaz.
                throw;
            }
            catch (Exception ex)
            {
                // Ağ hatası veya parse hatası → bilinmiyor, "bulunamadı" OLMAZ.
                // Rethrow ederek çağıranın bu sevkiyatı "missing" saymamasını sağla.
                _logger.LogWarning(ex, "TryGetSiparis ({FatIrsNo}) hatası — aktarım durumu sıfırlanmayacak", fatIrsNo);
                throw;
            }
        }

        /// <summary>
        /// Verilen URL'yi GET ile sorgular; 200 + geçerli JSON döndürdüyse true.
        /// </summary>
        private async Task<bool> SiparisExistsAtUrlAsync(string url, CancellationToken ct)
        {
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return false;

                var body = await resp.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(body) || body == "null") return false;

                var trimmed = body.TrimStart();
                if (trimmed.StartsWith('{') && trimmed.Contains("\"IsSuccessful\""))
                {
                    var apiResp = System.Text.Json.JsonSerializer.Deserialize<NetsisApiResponse>(body);
                    return apiResp?.IsSuccessful == true;
                }
                return trimmed.StartsWith('[') || trimmed.StartsWith('{');
            }
            catch { return false; }
        }

        public async Task<IReadOnlyDictionary<string, decimal>> GetStockKdvRatesAsync(
            IEnumerable<string> netsisStokKodlari,
            CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var codes  = netsisStokKodlari
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codes.Count == 0) return result;

            var token = await EnsureTokenAsync(cancellationToken);
            SetAuthHeader(token);

            var inList = string.Join(",", codes.Select(c => $"'{c.Replace("'", "''")}'"));
            var tsql   = $"SELECT STOK_KODU, STK_KDV FROM TBLSTKMAS WHERE STOK_KODU IN ({inList})";
            var url    = $"{_opt.StokBakiyePath}?tsql={HttpUtility.UrlEncode(tsql)}";

            try
            {
                using var resp = await _http.GetAsync(url, cancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GetStockKdvRates: HTTP {Status}", (int)resp.StatusCode);
                    return result;
                }

                var body = await resp.Content.ReadAsStringAsync(cancellationToken);
                var queryResp = System.Text.Json.JsonSerializer.Deserialize<NetsisQueryResponse>(body);

                if (queryResp is not { IsSuccessful: true })
                {
                    _logger.LogWarning("GetStockKdvRates: [{Code}] {Desc}", queryResp?.ErrorCode, queryResp?.ErrorDesc);
                    return result;
                }

                foreach (var row in queryResp.Data)
                {
                    string? stokKodu = null;
                    decimal kdv      = 0;

                    foreach (var kv in row)
                    {
                        if (kv.Key.Equals("STOK_KODU", StringComparison.OrdinalIgnoreCase) &&
                            kv.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            stokKodu = kv.Value.GetString()?.Trim();
                        }
                        else if (kv.Key.Equals("STK_KDV", StringComparison.OrdinalIgnoreCase) &&
                                 kv.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            kdv = kv.Value.GetDecimal();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(stokKodu))
                        result[stokKodu] = kdv;
                }

                _logger.LogInformation(
                    "GetStockKdvRates: {Count} stok kodu sorgulandı, {Found} KDV oranı döndü.",
                    codes.Count, result.Count);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetStockKdvRates: sorgu başarısız — fallback uygulanacak.");
            }

            return result;
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

            // Response: {"IsSuccessful":true,"Data":[{"Column1":123.45},...]}
            decimal mevcutStok = 0;
            try
            {
                var queryResp = System.Text.Json.JsonSerializer.Deserialize<NetsisQueryResponse>(raw);
                if (queryResp is not null)
                {
                    if (!queryResp.IsSuccessful)
                        throw new HttpRequestException(
                            $"Netsis GetStockBalance hatası: [{queryResp.ErrorCode}] {queryResp.ErrorDesc}");

                    if (queryResp.Data.Count > 0)
                    {
                        var firstRow = queryResp.Data[0];
                        foreach (var kv in firstRow)
                        {
                            if (kv.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                            {
                                mevcutStok = kv.Value.GetDecimal();
                                break;
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException) { throw; }
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
