namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>Bir FreightDelivery'nin (proje teslim linki) kapsadığı sevkiyatlar.</summary>
    public class FreightDeliveryShipment
    {
        public int Id { get; private set; }

        public Guid FreightDeliveryId { get; private set; }
        public FreightDelivery FreightDelivery { get; private set; } = null!;

        public int ShipmentId { get; private set; }
        public Shipment Shipment { get; private set; } = null!;

        private FreightDeliveryShipment() { }

        public static FreightDeliveryShipment Create(Guid freightDeliveryId, int shipmentId)
            => new() { FreightDeliveryId = freightDeliveryId, ShipmentId = shipmentId };
    }
}
