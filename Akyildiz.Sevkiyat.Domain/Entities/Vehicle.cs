using System.Collections.Generic;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = null!;
        public string? Capacity { get; set; } // Description like "Tır", "Kamyonet"
        public bool IsActive { get; set; } = true;
    }
}
