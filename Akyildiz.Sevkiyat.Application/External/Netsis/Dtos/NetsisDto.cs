namespace Akyildiz.Sevkiyat.Application.External.Netsis.Dtos
{
    // ---------------------------------------------------------------------------
    // TODO: NETSIS_API — Aşağıdaki tüm alan adları, Netsis tarafından sağlanacak
    // API dokümantasyonu ile güncellenmeli. Şu an mantıksal isimlerle yer tutucu.
    // ---------------------------------------------------------------------------

    // ── Login ──────────────────────────────────────────────────────────────────

    public sealed class NetsisLoginRequest
    {
        // TODO: NETSIS_API — login endpoint field names
        public string KullaniciAdi { get; set; } = string.Empty;
        public string Sifre       { get; set; } = string.Empty;
        public string FirmaKodu   { get; set; } = string.Empty;
        public string SubeKodu    { get; set; } = string.Empty;
        public string? IsletmeKodu { get; set; }
    }

    public sealed class NetsisLoginResponse
    {
        // TODO: NETSIS_API — token field name (örn. "token", "accessToken", "result")
        public string Token { get; set; } = string.Empty;

        // TODO: NETSIS_API — expiry field (varsa), yoksa NetsisOptions.TokenExpiryMinutes kullanılır
        public DateTime? ExpireDate { get; set; }
    }

    // ── Müşteri Siparişi ───────────────────────────────────────────────────────

    public sealed class NetsisSiparisRequest
    {
        // TODO: NETSIS_API — belge no field name (bizim TalepNo / ExternalOrderNumber)
        public string BelgeNo     { get; set; } = string.Empty;
        public string CariKodu    { get; set; } = string.Empty;   // Project.NetsisCariKodu
        public string ProjeKodu   { get; set; } = string.Empty;   // Project.Code
        public string? DepoKodu   { get; set; }                   // NetsisOptions.DepoKodu
        public DateTime TeslimTarihi { get; set; }

        // TODO: NETSIS_API — satır array field name
        public List<NetsisSiparisLine> Satirlar { get; set; } = new();
    }

    public sealed class NetsisSiparisLine
    {
        public int    SatirNo  { get; set; }
        // TODO: NETSIS_API — stok kodu field name
        public string StokKodu { get; set; } = string.Empty;
        public decimal Miktar  { get; set; }
        public string? Birim   { get; set; }

        // Fiyat alanları — Netsis kabul ediyorsa gönderilir, yoksa boş bırakılır
        public decimal? BirimFiyati { get; set; }
        public decimal? KdvOrani   { get; set; }
    }

    public sealed class NetsisSiparisResult
    {
        // TODO: NETSIS_API — başarı/hata alanı ve dönen Netsis sipariş no
        public bool   Basarili       { get; set; }
        public string? NetsisOrderNo { get; set; }
        public string? Mesaj         { get; set; }
    }

    // ── Satınalma Siparişi ─────────────────────────────────────────────────────

    public sealed class NetsisPoRequest
    {
        // TODO: NETSIS_API — belge no field name
        public string BelgeNo          { get; set; } = string.Empty; // PurchaseOrder.OrderNumber
        public string TedarikciKodu    { get; set; } = string.Empty; // Supplier.SupplierCode
        public DateOnly SiparisDate    { get; set; }
        public DateOnly? TeslimTarihi  { get; set; }
        public string? Aciklama        { get; set; }

        // TODO: NETSIS_API — satır array field name
        public List<NetsisPoLine> Satirlar { get; set; } = new();
    }

    public sealed class NetsisPoLine
    {
        public int    SatirNo  { get; set; }
        // TODO: NETSIS_API — stok kodu field name
        public string StokKodu { get; set; } = string.Empty; // StockMaster.NetsisStockCode
        public decimal Miktar  { get; set; }
        public string? Birim   { get; set; }
    }

    public sealed class NetsisPoResult
    {
        // TODO: NETSIS_API — başarı/hata alanı ve dönen Netsis PO no
        public bool    Basarili    { get; set; }
        public string? NetsisPONo  { get; set; }
        public string? Mesaj       { get; set; }
    }

    // ── İrsaliye ──────────────────────────────────────────────────────────────

    public sealed class NetsisIrsaliyeQuery
    {
        // TODO: NETSIS_API — sorgu parametre isimleri
        public string? SiparisNo { get; set; }  // IssOrder.NetsisOrderNumber
        public DateOnly? BaslangicTarihi { get; set; }
        public DateOnly? BitisTarihi     { get; set; }
    }

    public sealed class NetsisIrsaliyeDto
    {
        // TODO: NETSIS_API — response alan isimleri
        public string   IrsaliyeNo    { get; set; } = string.Empty;
        public DateOnly IrsaliyeTarihi { get; set; }
        public string?  SiparisNo     { get; set; }
        public decimal? Tutar         { get; set; }
    }

    // ── Stok Bakiye ───────────────────────────────────────────────────────────

    public sealed class NetsisStockBalanceQuery
    {
        // TODO: NETSIS_API — sorgu parametre isimleri
        public string? StokKodu { get; set; }
        public string? DepoKodu { get; set; }
    }

    public sealed class NetsisStockBalanceDto
    {
        // TODO: NETSIS_API — response alan isimleri
        public string  StokKodu        { get; set; } = string.Empty;
        public string? DepoKodu        { get; set; }
        public decimal MevcutStok      { get; set; }  // OnHandQty equivalent
        public decimal RezerveStok     { get; set; }  // ReservedQty equivalent
        public decimal SerbstStok      { get; set; }  // AvailableQty equivalent
    }
}
