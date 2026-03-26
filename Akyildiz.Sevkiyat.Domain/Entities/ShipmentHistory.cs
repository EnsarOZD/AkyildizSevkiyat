using Akyildiz.Sevkiyat.Domain.Enums;
using System;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class ShipmentHistory
    {
        public int Id { get; set; }
        public int ShipmentId { get; set; }
        
        public ShipmentStatus OldStatus { get; set; }
        public ShipmentStatus NewStatus { get; set; }
        

        public string? Description { get; set; } // Log extra details
        
        public int? ChangedByUserId { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
