namespace Akyildiz.Sevkiyat.Domain.Entities
{
    /// <summary>Bir nakliyeciye ait araç plakası.</summary>
    public class CarrierVehicle
    {
        public int Id { get; set; }
        public int CarrierId { get; set; }
        public Carrier Carrier { get; set; } = null!;
        public string PlateNumber { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}
