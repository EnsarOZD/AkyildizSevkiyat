namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Mal kabul sonrası oluşturulan dağıtım görevi.
    /// Her GoodsReceiptLine için bir PutawayTask oluşturulur.
    /// Ürünü çeşitli lokasyonlara (raf, toplama gözü) dağıtmayı izler.
    /// </summary>
    public class PutawayTask
    {
        public int Id { get; set; }

        public Guid GoodsReceiptId { get; set; }
        public GoodsReceipt GoodsReceipt { get; set; } = null!;

        public Guid GoodsReceiptLineId { get; set; }
        public GoodsReceiptLine GoodsReceiptLine { get; set; } = null!;

        public int StockMasterId { get; set; }
        public StockMaster StockMaster { get; set; } = null!;

        /// <summary>Dağıtılacak toplam miktar (GoodsReceiptLine.AcceptedQty'dan kopyalanır).</summary>
        public decimal TotalQty { get; set; }

        /// <summary>Şimdiye kadar lokasyonlara dağıtılan toplam miktar.</summary>
        public decimal DistributedQty { get; set; } = 0;

        public decimal RemainingQty => TotalQty - DistributedQty;

        public PutawayTaskStatus Status { get; set; } = PutawayTaskStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public ICollection<PutawayLine> Lines { get; set; } = new List<PutawayLine>();
    }

    /// <summary>
    /// Bir PutawayTask içindeki tek bir dağıtım satırı.
    /// Belirli bir lokasyona belirli bir miktar atamayı temsil eder.
    /// </summary>
    public class PutawayLine
    {
        public int Id { get; set; }

        public int PutawayTaskId { get; set; }
        public PutawayTask PutawayTask { get; set; } = null!;

        public decimal Qty { get; set; }

        public int WarehouseLocationId { get; set; }
        public WarehouseLocation WarehouseLocation { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByUserId { get; set; }
    }

    public enum PutawayTaskStatus
    {
        Pending             = 0, // Hiç dağıtılmadı
        PartiallyDistributed = 1, // Kısmen dağıtıldı
        Completed           = 2, // Tamamı dağıtıldı
    }
}
