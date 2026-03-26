namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Lokasyonlar arası stok hareketi kaydı.
    /// </summary>
    public class LocationTransfer
    {
        public int Id { get; set; }

        public int StockMasterId { get; set; }
        public StockMaster StockMaster { get; set; } = null!;

        public int FromLocationId { get; set; }
        public WarehouseLocation FromLocation { get; set; } = null!;

        public int ToLocationId { get; set; }
        public WarehouseLocation ToLocation { get; set; } = null!;

        public decimal Qty { get; set; }

        public string? Note { get; set; }

        public int? TransferredByUserId { get; set; }

        public DateTime TransferredAt { get; set; } = DateTime.UtcNow;
    }
}
