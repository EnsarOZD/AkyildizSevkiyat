using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class FloatingReturn
    {
        public int Id { get; set; }

        public DateTime ReturnDate { get; set; }

        // Stok bilgisi — eşleşmişse FK, eşleşmemişse serbest metin
        public int? StockMasterId { get; set; }
        public StockMaster? StockMaster { get; set; }
        public string? StockCodeFree { get; set; }   // StockMaster yoksa
        public string? StockNameFree { get; set; }

        public decimal Qty { get; set; }
        public ReturnReason ReturnReason { get; set; } = ReturnReason.Other;
        public string? Note { get; set; }

        public FloatingReturnStatus Status { get; set; } = FloatingReturnStatus.Pending;

        // Eşleştirme sonucu dolar
        public int? LinkedShipmentId { get; set; }
        public Shipment? LinkedShipment { get; set; }

        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByUserId { get; set; }
    }

    public enum FloatingReturnStatus
    {
        Pending            = 0,  // Çözüm bekliyor
        MatchedToShipment  = 1,  // Sevkiyata eşleştirildi
        AddedToStock       = 2,  // Stoğa eklendi
        WrittenOff         = 3   // Hariç tutuldu
    }
}
