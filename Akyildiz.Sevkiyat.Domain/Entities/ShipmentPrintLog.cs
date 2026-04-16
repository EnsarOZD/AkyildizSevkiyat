namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class ShipmentPrintLog
    {
        public int Id { get; set; }
        public int ShipmentId { get; set; }
        public DateTime PrintedAt { get; set; }
        public int? PrintedByUserId { get; set; }
        public string PrintedByName { get; set; } = string.Empty;

        // Navigation
        public Shipment Shipment { get; set; } = null!;
    }
}
