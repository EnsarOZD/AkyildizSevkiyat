using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Toplama/kapama sırasında eksik kalan ürün kaydı. Bağımsız bir kuyrukta yaşar;
    /// "Gönder" denildiğinde yalnızca eksik satırları içeren bir tamamlama Shipment'ı üretilir.
    /// </summary>
    public class ShortageRecord
    {
        public int Id { get; set; }

        public int ShipmentId { get; set; }            // Eksik çıkan kaynak sevkiyat
        public Shipment Shipment { get; set; } = null!;
        public int? ShipmentLineId { get; set; }       // Orijinal eksik satır (izlenebilirlik)

        public int? StockMasterId { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;

        public int ProjectId { get; set; }             // Kuyruk görünümü/filtre
        public string ProjectName { get; set; } = string.Empty;

        public decimal Qty { get; set; }               // Eksik miktar

        public ShortageStatus Status { get; set; } = ShortageStatus.Pending;
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByUserId { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByUserId { get; set; }

        public int? FollowupShipmentId { get; set; }   // "Gönder" ile oluşan tamamlama sevkiyatı
    }
}
