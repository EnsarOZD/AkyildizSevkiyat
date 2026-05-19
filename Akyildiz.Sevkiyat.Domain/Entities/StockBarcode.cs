namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Bir stok kalemine ait ek barkodlar.
    /// Aynı ürün farklı tedarikçi ambalajlarında farklı barkodlarla gelebilir.
    /// </summary>
    public class StockBarcode
    {
        public int Id { get; set; }

        public int StockMasterId { get; set; }
        public StockMaster StockMaster { get; set; } = null!;

        /// <summary>Barkod değeri (EAN13, Code128, QR vb.)</summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>Opsiyonel açıklama. Örn: "Tedarikçi A ambalajı", "5 kg koli"</summary>
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
