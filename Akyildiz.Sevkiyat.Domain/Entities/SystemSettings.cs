namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>Singleton sistem ayarları (Id=1 her zaman).</summary>
    public class SystemSettings
    {
        public int Id { get; set; } = 1;
        public string? DepotName { get; set; }
        public string? DepotAddress { get; set; }
        public double? DepotLatitude { get; set; }
        public double? DepotLongitude { get; set; }

        /// <summary>Satınalma siparişi e-postalarına CC olarak eklenecek adresler (virgülle ayrılmış).</summary>
        public string? ProcurementEmailCc { get; set; }

        /// <summary>Sevkiyat/eksik ürün bildirim e-postalarına CC olarak eklenecek adresler (virgülle ayrılmış).</summary>
        public string? DispatchEmailCc { get; set; }

        /// <summary>Şoföre atama yapıldığında otomatik e-posta gönderimini etkinleştirir/devre dışı bırakır.</summary>
        public bool DispatchEmailEnabled { get; set; } = true;

        // ── WMS Depo Yönetimi Ayarları ────────────────────────────────────────

        /// <summary>
        /// Mal kabul onaylandıktan sonra dağıtım (putaway) ekranını aktif eder.
        /// Kapalıyken mevcut mal kabul akışı değişmez.
        /// </summary>
        public bool WmsPutawayEnabled { get; set; } = false;

        /// <summary>
        /// Picking sırasında sistem toplama gözü / raf lokasyon önerisi yapar
        /// ve StockLocation bazında stok düşümü gerçekleştirir.
        /// Kapalıyken yalnızca StockMaster bazında düşüm yapılır (mevcut davranış).
        /// </summary>
        public bool WmsLocationPickingEnabled { get; set; } = false;

        /// <summary>
        /// Toplama sırasında ürün barkodu tarama zorunlu olur.
        /// Kapalıyken manuel miktar girişiyle toplama yapılabilir (mevcut davranış).
        /// </summary>
        public bool WmsBarcodePickingEnabled { get; set; } = false;
    }
}
