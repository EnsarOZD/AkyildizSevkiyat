namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class ZonePreparationDriver
    {
        public int Id { get; set; }
        public int ZonePreparationId { get; set; }
        public int DriverId { get; set; }
        public bool IsPrimary { get; set; } = false;

        public ZonePreparation ZonePreparation { get; set; } = null!;
        public Driver Driver { get; set; } = null!;
    }
}
