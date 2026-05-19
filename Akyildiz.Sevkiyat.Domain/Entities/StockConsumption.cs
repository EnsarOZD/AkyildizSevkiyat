using Akyildiz.Sevkiyat.Domain.Common;
using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class StockConsumption : AuditableEntity
    {
        public int Id { get; set; }

        public int StockMasterId { get; set; }
        public StockMaster StockMaster { get; set; } = null!;

        // Snapshots
        public string StockCodeSnapshot { get; set; } = string.Empty;
        public string StockNameSnapshot { get; set; } = string.Empty;
        public StockUnit UnitSnapshot { get; set; }

        public StockConsumptionType Type { get; set; }
        public decimal Quantity { get; set; }
        public DateOnly Date { get; set; }

        // Zai
        public string? Reason { get; set; }

        // Dahili Kullanım
        public string? RecipientName { get; set; }

        // Depo Satışı
        public decimal? SalePrice { get; set; }

        public string? Note { get; set; }
    }
}
