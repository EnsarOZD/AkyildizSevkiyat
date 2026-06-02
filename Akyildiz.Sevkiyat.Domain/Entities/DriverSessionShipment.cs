namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>
    /// Bir seferin (DriverSession) hangi sevkiyatları taşıdığını kalıcı olarak tutan manifest.
    /// Sefer başında, okutulan irsaliyeden çözülen sevkiyatlar buraya yazılır; böylece
    /// "bu seferde bu sevkiyatlar vardı" sorusu geçmişe dönük net cevaplanır.
    /// </summary>
    public class DriverSessionShipment
    {
        public int Id { get; private set; }

        public Guid DriverSessionId { get; private set; }
        public DriverSession DriverSession { get; private set; } = null!;

        public int ShipmentId { get; private set; }
        public Shipment Shipment { get; private set; } = null!;

        public DateTime AddedAt { get; private set; }

        private DriverSessionShipment() { }

        public static DriverSessionShipment Create(Guid driverSessionId, int shipmentId)
        {
            return new DriverSessionShipment
            {
                DriverSessionId = driverSessionId,
                ShipmentId = shipmentId,
                AddedAt = DateTime.UtcNow,
            };
        }
    }
}
