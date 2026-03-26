namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class StockCount
    {
        public int Id { get; set; }

        public DateTime CountDate { get; set; }
        public string? Note { get; set; }

        public StockCountStatus Status { get; set; } = StockCountStatus.Draft;

        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }
        public int? CompletedByUserId { get; set; }

        public ICollection<StockCountLine> Lines { get; set; } = new List<StockCountLine>();
    }

    public enum StockCountStatus
    {
        Draft     = 0,  // Sayım devam ediyor
        Completed = 1,  // Sayım tamamlandı, fark fişi oluşturuldu
        Cancelled = 2   // Sayım iptal edildi
    }
}
