using System;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class ZonePreparationProject
    {
        public int Id { get; set; }

        public int ZonePreparationId { get; set; }
        public ZonePreparation ZonePreparation { get; set; } = null!;

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public bool IsMicroReady { get; set; } = false;
        public DateTime? MicroReadyAt { get; set; }
        public string? PreparedByUserName { get; set; }

        public bool IsAddedLater { get; set; } = false;

        /// <summary>Driver-adjustable delivery order for this zone preparation run. Null until first zone driver assignment.</summary>
        public int? RouteOrder { get; set; }

        // Picking lock — prevents two users picking the same project simultaneously
        public int? PickingLockedByUserId { get; set; }
        public string? PickingLockedByUserName { get; set; }
        public DateTime? PickingLockedAt { get; set; }

        private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(60);

        public bool IsPickingLocked(int requestingUserId) =>
            PickingLockedByUserId.HasValue &&
            PickingLockedByUserId.Value != requestingUserId &&
            PickingLockedAt.HasValue &&
            DateTime.UtcNow - PickingLockedAt.Value < LockExpiry;

        public void AcquireLock(int userId, string userName)
        {
            PickingLockedByUserId   = userId;
            PickingLockedByUserName = userName;
            PickingLockedAt         = DateTime.UtcNow;
        }

        public void ReleaseLock()
        {
            PickingLockedByUserId   = null;
            PickingLockedByUserName = null;
            PickingLockedAt         = null;
        }
    }
}
