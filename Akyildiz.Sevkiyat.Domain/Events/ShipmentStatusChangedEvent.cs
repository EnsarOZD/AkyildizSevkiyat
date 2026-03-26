using Akyildiz.Sevkiyat.Domain.Common;
using Akyildiz.Sevkiyat.Domain.Enums;
using System;

namespace Akyildiz.Sevkiyat.Domain.Events
{
    public class ShipmentStatusChangedEvent : IDomainEvent
    {
        public ShipmentStatusChangedEvent(int shipmentId, ShipmentStatus oldStatus, ShipmentStatus newStatus, int? changedByUserId)
        {
            ShipmentId = shipmentId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            ChangedByUserId = changedByUserId;
            OccurredOn = DateTime.UtcNow;
        }

        public int ShipmentId { get; }
        public ShipmentStatus OldStatus { get; }
        public ShipmentStatus NewStatus { get; }
        public int? ChangedByUserId { get; }
        public DateTime OccurredOn { get; }
    }
}
