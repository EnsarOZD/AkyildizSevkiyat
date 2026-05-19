namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class ShipmentDeliveryPhoto
    {
        public int Id { get; set; }
        public int ShipmentId { get; set; }
        public Shipment Shipment { get; set; } = null!;

        /// <summary>Relative path on disk (e.g. "delivery/2026/05/17/123_IRS001_1_a1b2c3d4.jpg")</summary>
        public string PhotoPath { get; set; } = string.Empty;

        /// <summary>1-based index within this shipment's delivery photos (1–5)</summary>
        public int PhotoIndex { get; set; }

        public DateTime TakenAt { get; set; } = DateTime.UtcNow;
    }
}
