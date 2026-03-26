using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class StockMapping
    {
        public int Id { get; set; }

        public string ExternalSystem { get; set; } = "ISS-IP";
        public string ExternalStockCode { get; set; } = null!;
        public string ExternalStockName { get; set; } = null!;

        public int? LocalStockId { get; set; }
        public StockMaster? LocalStock { get; set; }

        public MatchStatus MatchStatus { get; set; }
    }
}
