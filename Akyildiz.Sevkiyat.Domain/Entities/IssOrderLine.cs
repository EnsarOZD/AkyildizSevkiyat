using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class IssOrderLine
    {
        public int Id { get; set; }

        public int IssOrderId { get; set; }
        public IssOrder IssOrder { get; set; } = null!;

        public int LineNumber { get; set; }          // ISS/Netsis satır no (varsa)

        public string StockCode { get; set; } = null!;
        public string StockName { get; set; } = null!;
        public StockUnit Unit { get; set; } = StockUnit.Adet;

        public decimal OrderedQty { get; set; }      // ISS’in istediği miktar

        // Pricing Fields
        public decimal? ListeFiyati { get; set; }
        public decimal? Iskonto { get; set; }
        public decimal? BirimFiyati { get; set; }
        public decimal? KDVOrani { get; set; }

        public ICollection<ShipmentLine> ShipmentLines { get; set; } = new List<ShipmentLine>();
    }
}
