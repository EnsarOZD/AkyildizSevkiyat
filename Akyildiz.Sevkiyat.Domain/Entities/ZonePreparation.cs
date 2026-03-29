using Akyildiz.Sevkiyat.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class ZonePreparation
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        public DateTime DeliveryDate { get; set; }
        
        public ZonePreparationStatus Status { get; set; } = ZonePreparationStatus.Draft;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Driver & Vehicle (Optional until ReadyForTransfer)
        public int? DriverId { get; set; }
        public Driver? Driver { get; set; }

        public int? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public int BatchNo { get; set; } = 1;
        public bool IsFrozen { get; set; } = false;
        public bool IrsaliyeFetched { get; set; } = false;
        public DateTime? StartedAt { get; set; }
        public int? StartedByUserId { get; set; }

        // Navigation Properties
        public Zone Zone { get; set; } = null!;
        public ICollection<ZonePreparationProject> Projects { get; set; } = new List<ZonePreparationProject>();
        public ICollection<ZonePreparationDriver> DriverAssignments { get; set; } = new List<ZonePreparationDriver>();
    }
}
