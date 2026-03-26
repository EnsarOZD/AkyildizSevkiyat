namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Bir stok kaleminin belirli bir depo lokasyonundaki mevcut miktarını tutar.
    /// Bir stok birden fazla lokasyonda bulunabilir.
    /// </summary>
    public class StockLocation
    {
        public int Id { get; set; }

        public int StockMasterId { get; set; }
        public StockMaster StockMaster { get; set; } = null!;

        public int WarehouseLocationId { get; set; }
        public WarehouseLocation WarehouseLocation { get; set; } = null!;

        public decimal OnHandQty { get; set; } = 0;
        public decimal ReservedQty { get; set; } = 0;
        public decimal AvailableQty => OnHandQty - ReservedQty;

        public DateTime? LastMovedAt { get; set; }
    }
}
