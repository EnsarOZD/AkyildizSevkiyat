using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = null!;
        public string? Capacity { get; set; }
        public VehicleType VehicleType { get; set; } = VehicleType.Kamyon;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
