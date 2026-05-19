using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class VehicleReturnLine
    {
        public int Id { get; set; }
        public int VehicleReturnId { get; set; }
        public VehicleReturn VehicleReturn { get; set; } = null!;

        public int? StockMasterId { get; set; }
        public StockMaster? StockMaster { get; set; }
        public string? StockCodeFree { get; set; }
        public string? StockNameFree { get; set; }

        public decimal Qty { get; set; }
        public string? Note { get; set; }

        public VehicleReturnLineStatus Status { get; set; } = VehicleReturnLineStatus.Pending;

        public int? LinkedShipmentId { get; set; }
        public Shipment? LinkedShipment { get; set; }

        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByUserId { get; set; }
    }
}
