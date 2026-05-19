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

        // Macro picking lock — prevents two users doing macro for the same zone simultaneously
        public int? MacroLockedByUserId { get; set; }
        public string? MacroLockedByUserName { get; set; }
        public DateTime? MacroLockedAt { get; set; }

        private static readonly TimeSpan MacroLockExpiry = TimeSpan.FromMinutes(60);

        public bool IsMacroLocked(int requestingUserId) =>
            MacroLockedByUserId.HasValue &&
            MacroLockedByUserId.Value != requestingUserId &&
            MacroLockedAt.HasValue &&
            DateTime.UtcNow - MacroLockedAt.Value < MacroLockExpiry;

        public void AcquireMacroLock(int userId, string userName)
        {
            MacroLockedByUserId   = userId;
            MacroLockedByUserName = userName;
            MacroLockedAt         = DateTime.UtcNow;
        }

        public void ReleaseMacroLock()
        {
            MacroLockedByUserId   = null;
            MacroLockedByUserName = null;
            MacroLockedAt         = null;
        }

        // Navigation Properties
        public Zone Zone { get; set; } = null!;
        public ICollection<ZonePreparationProject> Projects { get; set; } = new List<ZonePreparationProject>();
        public ICollection<ZonePreparationDriver> DriverAssignments { get; set; } = new List<ZonePreparationDriver>();
    }
}
