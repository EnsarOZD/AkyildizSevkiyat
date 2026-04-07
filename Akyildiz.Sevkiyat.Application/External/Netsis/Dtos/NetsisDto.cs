using System.Text.Json.Serialization;

namespace Akyildiz.Sevkiyat.Application.External.Netsis.Dtos
{
    // ── Login ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/v2/token?grant_type=password&amp;branchcode=...
    /// Form-urlencoded query string olarak gönderilir — NetsisClient tarafından oluşturulur.
    /// </summary>
    public sealed class NetsisLoginResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    // ── Wire Format — ItemSlips ────────────────────────────────────────────────

    /// <summary>
    /// POST /api/v2/ItemSlips — hem müşteri siparişi (FaturaTip=7) hem satınalma (FaturaTip=6) için.
    /// </summary>
    public sealed class NetsisCreateItemSlipRequest
    {
        public string Seri                               { get; set; } = "0";
        public int    FaturaTip                          { get; set; }
        public bool   KosulluHesapla                     { get; set; } = false;
        public bool   FiyatSistemineGoreHesapla          { get; set; } = false;
        public bool   OtoVadeGunGetir                    { get; set; } = true;
        public bool   SeriliHesapla                      { get; set; } = true;
        public bool   OtomatikCevrimYapilsin             { get; set; } = false;
        public bool   KayitliNumaraOtomatikGuncellensin  { get; set; } = false;
        public bool   RiskKontrol                        { get; set; } = true;
        public NetsisFatUst               FatUst         { get; set; } = new();
        public List<NetsisKalem>          Kalems         { get; set; } = new();
    }

    public sealed class NetsisFatUst
    {
        public string  Sube_Kodu   { get; set; } = string.Empty;
        public string  FATIRS_NO   { get; set; } = string.Empty;
        public string  CariKod     { get; set; } = string.Empty;
        public string  CARI_KOD2   { get; set; } = string.Empty;
        public string  Tarih       { get; set; } = string.Empty; // "yyyy-MM-dd HH:mm:ss"
        public int     Tip         { get; set; }
        public int     TIPI        { get; set; }
        public string  ENTEGRE_TRH { get; set; } = string.Empty;
        public bool    KDV_DAHILMI { get; set; } = false;
        public string  SIPARIS_TEST { get; set; } = string.Empty; // TeslimTarihi
        public string  D_YEDEK10   { get; set; } = string.Empty; // İstenen teslim tarihi
        public string  FIYATTARIHI { get; set; } = string.Empty;
        public string  KOSULTARIHI { get; set; } = string.Empty;

        // EKACK alanları — yalnızca müşteri siparişinde (FaturaTip=7) kullanılır.
        // Satınalma siparişinde (FaturaTip=6) null bırakılır → JSON'a yazılmaz.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EKACK1  { get; set; }  // SiparisId

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EKACK2  { get; set; }  // KurumKodu

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EKACK3  { get; set; }  // TalepNo

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EKACK4  { get; set; }  // TalepTuru

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EKACK6  { get; set; }  // Donem (max 100 char)

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EKACK7  { get; set; }  // TeslimAlacakKisiler (max 100 char)

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EKACK8  { get; set; }  // TeslimAlacakTelefonNumaralari (max 100 char)

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EKACK9  { get; set; }  // YoneticiMailAdresleri (max 100 char)

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EKACK16 { get; set; }  // ElTerminaliFlag ("0")
    }

    public sealed class NetsisKalem
    {
        public string  StokKodu      { get; set; } = string.Empty;
        public string  DEPO_KODU     { get; set; } = string.Empty;
        public decimal STra_GCMIK    { get; set; }  // Miktar
        public decimal STra_BF       { get; set; }  // BirimFiyati
        public decimal STra_KDV      { get; set; }  // KdvOrani
        public string  STra_testar   { get; set; } = string.Empty; // TeslimTarihi "yyyy-MM-dd HH:mm:ss"
        public string  Stra_KosTar   { get; set; } = string.Empty; // Sipariş tarihi
        public string  Stra_FiyatTar { get; set; } = string.Empty; // Sipariş tarihi
    }

    // ── Application-layer DTOs (command handler'ların kullandığı tipler) ───────

    public sealed class NetsisSiparisRequest
    {
        public string   BelgeNo      { get; set; } = string.Empty; // TalepNo / ExternalOrderNumber
        public string   CariKodu     { get; set; } = string.Empty; // Project.NetsisCariKodu
        public string   ProjeKodu    { get; set; } = string.Empty; // Project.Code
        public string?  DepoKodu     { get; set; }
        public DateTime TeslimTarihi { get; set; }

        // EKACK alanları
        public string? SiparisId                      { get; set; } // EKACK1 — Shipment.Id
        public string? KurumKodu                      { get; set; } // EKACK2 — Project.InstitutionCode
        public string? TalepNo                        { get; set; } // EKACK3
        public string? TalepTuru                      { get; set; } // EKACK4
        public string? Donem                          { get; set; } // EKACK6
        public string? TeslimAlacakKisiler            { get; set; } // EKACK7
        public string? TeslimAlacakTelefonNumaralari  { get; set; } // EKACK8
        public string? YoneticiMailAdresleri          { get; set; } // EKACK9

        public List<NetsisSiparisLine> Satirlar { get; set; } = new();
    }

    public sealed class NetsisSiparisLine
    {
        public int     SatirNo     { get; set; }
        public string  StokKodu    { get; set; } = string.Empty;
        public decimal Miktar      { get; set; }
        public string? Birim       { get; set; }
        public decimal BirimFiyati { get; set; }
        public decimal KdvOrani    { get; set; }
    }

    public sealed class NetsisSiparisResult
    {
        public bool    Basarili      { get; set; }
        public string? NetsisOrderNo { get; set; }
        public string? Mesaj         { get; set; }
    }

    // ── Satınalma Siparişi ─────────────────────────────────────────────────────

    public sealed class NetsisPoRequest
    {
        public string   BelgeNo        { get; set; } = string.Empty; // PurchaseOrder.OrderNumber
        public string   TedarikciKodu  { get; set; } = string.Empty; // Supplier.SupplierCode
        public string?  DepoKodu       { get; set; }
        public DateOnly SiparisDate    { get; set; }
        public DateOnly? TeslimTarihi  { get; set; }
        public string?  Aciklama       { get; set; }

        public List<NetsisPoLine> Satirlar { get; set; } = new();
    }

    public sealed class NetsisPoLine
    {
        public int     SatirNo  { get; set; }
        public string  StokKodu { get; set; } = string.Empty;
        public decimal Miktar   { get; set; }
        public string? Birim    { get; set; }
    }

    public sealed class NetsisPoResult
    {
        public bool    Basarili   { get; set; }
        public string? NetsisPONo { get; set; }
        public string? Mesaj      { get; set; }
    }

    // ── İrsaliye ──────────────────────────────────────────────────────────────

    public sealed class NetsisIrsaliyeQuery
    {
        public string? SiparisNo       { get; set; } // FATIRS_NO formatında
        public string? CariKod         { get; set; } // Netsis cari kodu
        public DateOnly? BaslangicTarihi { get; set; }
        public DateOnly? BitisTarihi     { get; set; }
    }

    public sealed class NetsisIrsaliyeDto
    {
        [JsonPropertyName("FATIRS_NO")]
        public string IrsaliyeNo { get; set; } = string.Empty;

        [JsonPropertyName("Tarih")]
        public string? TarihStr { get; set; }

        [JsonPropertyName("SIPARIS_NO")]
        public string? SiparisNo { get; set; }

        // Computed — TarihStr'den parse edilir
        public DateOnly IrsaliyeTarihi =>
            DateOnly.TryParse(TarihStr, out var d) ? d : DateOnly.MinValue;
    }

    // ── Stok Bakiye ───────────────────────────────────────────────────────────

    public sealed class NetsisStockBalanceDto
    {
        public string  StokKodu   { get; set; } = string.Empty;
        public string? DepoKodu   { get; set; }
        public decimal MevcutStok { get; set; }
    }
}
