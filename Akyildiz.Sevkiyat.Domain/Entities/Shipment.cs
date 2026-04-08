using Akyildiz.Sevkiyat.Domain.Common;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using Akyildiz.Sevkiyat.Domain.Events;
using System.ComponentModel.DataAnnotations.Schema;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class Shipment : IHasDomainEvents
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public DateTime DeliveryDate { get; set; }

        public ShipmentStatus Status { get; private set; } = ShipmentStatus.Created;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public ICollection<ShipmentLine> Lines { get; protected set; } = new List<ShipmentLine>();

        public int IssOrderId { get; set; }
        public IssOrder IssOrder { get; set; } = null!;

        // Warehouse Batch Link
        public int? ZonePreparationId { get; set; }
        public ZonePreparation? ZonePreparation { get; set; }

        public ICollection<ShipmentHistory> Histories { get; private set; } = new List<ShipmentHistory>();

        // Driver Info
        public string? AssignedDriverName { get; private set; }
        public string? AssignedPlateNumber { get; private set; }
        public int? AssignedDriverId { get; private set; }
        public Driver? AssignedDriver { get; set; }
        public string? TalepNo { get; set; }

        // Netsis / İrsaliye
        public string? IrsaliyeNo { get; private set; }
        public DateOnly? IrsaliyeDate { get; private set; }
        public DateTime? NetsisTransferredAt { get; private set; }

        // Delivery Proof
        public DateTime? DeliveredAt { get; private set; }
        public string? DeliveryNote { get; private set; }
        public string? DeliveryRecipient { get; private set; }
        public string? DeliveryPhotoBase64 { get; private set; }

        // Delivery Audit
        public int? DeliveredByUserId { get; private set; }
        public string? DeliveredByRole { get; private set; }
        public string? DeliveryOverrideNote { get; private set; }

        // Vehicle Return
        public DateTime? ReturnedAt { get; private set; }
        public string? ReturnNote { get; private set; }

        // Dispatch Confirmation (yükleme onayı)
        public DateTime? DispatchedAt { get; private set; }
        public string? DispatchConfirmedByName { get; private set; }

        // Optimistic concurrency
        public byte[] RowVersion { get; set; } = null!;

        // Stok rezervasyon durumu — double-reserve koruması
        public bool StockReserved { get; private set; } = false;

        /// <summary>
        /// Stok rezervasyonunu işaretler. İkinci çağrıda DomainException fırlatır.
        /// </summary>
        public void MarkStockReserved()
        {
            if (StockReserved)
                throw new DomainException($"Sevkiyat #{Id} için stok zaten rezerve edilmiş.");
            StockReserved = true;
        }

        /// <summary>
        /// Stok rezervasyonunu serbest bırakır (iptal/revert senaryoları).
        /// </summary>
        public void MarkStockReleased()
        {
            StockReserved = false;
        }

        // Domain Events Implementation
        private readonly List<IDomainEvent> _domainEvents = new();

        [NotMapped]
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
        public void ClearDomainEvents() => _domainEvents.Clear();

        public void ChangeStatus(ShipmentStatus newStatus, int? userId, string? reason = null)
        {
            if (Status == newStatus) return;

            ValidateTransition(newStatus);

            // Enforce mandatory reason for skip or revert
            bool isSkip = IsSkipTransition(Status, newStatus);
            bool isRevert = IsRevertTransition(Status, newStatus);

            if ((isSkip || isRevert) && string.IsNullOrWhiteSpace(reason))
            {
                var type = isSkip ? "Skip (Atlama)" : "Revert (Geri Alma)";
                throw new DomainException($"{type} işlemi için açıklama girmek zorunludur. Durum: {Status} -> {newStatus}");
            }

            var oldStatus = Status;
            Status = newStatus;

            // Add to Histories collection for atomic persistence
            Histories.Add(new ShipmentHistory
            {
                ShipmentId = Id,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow,
                Description = reason
            });

            AddDomainEvent(new ShipmentStatusChangedEvent(Id, oldStatus, newStatus, userId));
        }

        private bool IsSkipTransition(ShipmentStatus current, ShipmentStatus next)
        {
            if (current == ShipmentStatus.Created && next == ShipmentStatus.Picking) return true;
            if (current == ShipmentStatus.AssignedToWarehouse && next == ShipmentStatus.ReadyForDispatch) return true;
            return false;
        }

        private bool IsRevertTransition(ShipmentStatus current, ShipmentStatus next)
        {
            // Any transition back to 'Created' is a revert
            if (next == ShipmentStatus.Created && current != ShipmentStatus.Created) return true;
            return false;
        }

        public void SetDriverInfo(string driverName, string plateNumber, int? driverId = null)
        {
            AssignedDriverName = driverName;
            AssignedPlateNumber = plateNumber;
            if (driverId.HasValue)
                AssignedDriverId = driverId.Value;
        }

        public void SetPassive(int? userId)
        {
            if (Status != ShipmentStatus.Created)
                throw new DomainException("Only 'Created' (Draft) shipments can be set to Passive.");

            ChangeStatus(ShipmentStatus.Passive, userId);
        }

        public void SetActive(int? userId, string? reason = null)
        {
            if (Status != ShipmentStatus.Passive)
                throw new DomainException("Only 'Passive' shipments can be set to Active.");

            ChangeStatus(ShipmentStatus.Created, userId, reason);
        }

        public void UpdateDeliveryDate(DateTime newDate)
        {
            if (Status != ShipmentStatus.Created)
                throw new DomainException("Only 'Created' (Draft) shipments can be edited.");

            DeliveryDate = newDate;
            // Clear stale zone preparation link so sync can re-assign for the new date
            ZonePreparationId = null;
            ZonePreparation = null;
        }

        public void SetIrsaliyeInfo(string? irsaliyeNo, DateOnly? irsaliyeDate)
        {
            IrsaliyeNo = irsaliyeNo;
            IrsaliyeDate = irsaliyeDate;
        }

        public void MarkNetsisTransferred(DateTime transferredAt)
        {
            NetsisTransferredAt = transferredAt;
        }

        /// <summary>
        /// Kıyafet operasyonu: Netsis aktarımı sonrası sevkiyatı doğrudan Delivered'a taşır.
        /// Sadece Created durumundaki sevkiyatlara uygulanabilir.
        /// </summary>
        public void SkipToDelivered()
        {
            if (Status != ShipmentStatus.Created)
                throw new DomainException(
                    "Kıyafet operasyonu aktarımı sadece 'Oluşturuldu' durumundaki sevkiyatlar için yapılabilir.");

            var oldStatus = Status;
            Status = ShipmentStatus.Delivered;

            Histories.Add(new ShipmentHistory
            {
                ShipmentId = Id,
                OldStatus = oldStatus,
                NewStatus = ShipmentStatus.Delivered,
                ChangedAt = DateTime.UtcNow,
                Description = "Kıyafet operasyonu — Netsis aktarımı ile doğrudan teslim edildi."
            });

            AddDomainEvent(new ShipmentStatusChangedEvent(Id, oldStatus, ShipmentStatus.Delivered, null));
        }

        public void RecordDelivery(DateTime deliveredAt, string recipient, string? note,
            string? photoBase64, int? deliveredByUserId, string? deliveredByRole, string? overrideNote)
        {
            DeliveredAt = deliveredAt;
            DeliveryRecipient = recipient;
            DeliveryNote = note;
            DeliveryPhotoBase64 = photoBase64;
            DeliveredByUserId = deliveredByUserId;
            DeliveredByRole = deliveredByRole;
            DeliveryOverrideNote = overrideNote;
        }

        public void RecordVehicleReturn(DateTime returnedAt, string? returnNote)
        {
            ReturnedAt = returnedAt;
            ReturnNote = returnNote;
        }

        public void RecordDispatch(DateTime dispatchedAt, string confirmedByName)
        {
            DispatchedAt = dispatchedAt;
            DispatchConfirmedByName = confirmedByName;
        }

        private void ValidateTransition(ShipmentStatus newStatus)
        {
            bool isValid = false;

            switch (Status)
            {
                case ShipmentStatus.Created:
                    isValid = newStatus == ShipmentStatus.AssignedToWarehouse
                           || newStatus == ShipmentStatus.Picking // Allow Skip
                           || newStatus == ShipmentStatus.Cancelled
                           || newStatus == ShipmentStatus.Passive;
                    break;
                case ShipmentStatus.Passive:
                    isValid = newStatus == ShipmentStatus.Created
                           || newStatus == ShipmentStatus.Cancelled;
                    break;
                case ShipmentStatus.AssignedToWarehouse:
                    isValid = newStatus == ShipmentStatus.Picking
                           || newStatus == ShipmentStatus.ReadyForDispatch // Allow Skip
                           || newStatus == ShipmentStatus.Cancelled
                           || newStatus == ShipmentStatus.Created;
                    break;
                case ShipmentStatus.Picking:
                    isValid = newStatus == ShipmentStatus.ReadyForDispatch
                           || newStatus == ShipmentStatus.Cancelled
                           || newStatus == ShipmentStatus.Created; // Allow Revert
                    break;
                case ShipmentStatus.ReadyForDispatch:
                    isValid = newStatus == ShipmentStatus.AssignedToVehicle || newStatus == ShipmentStatus.Cancelled;
                    break;
                case ShipmentStatus.AssignedToVehicle:
                    isValid = newStatus == ShipmentStatus.Dispatched
                           || newStatus == ShipmentStatus.Delivered
                           || newStatus == ShipmentStatus.ReturnedToWarehouse
                           || newStatus == ShipmentStatus.Cancelled;
                    break;
                case ShipmentStatus.Dispatched:
                    isValid = newStatus == ShipmentStatus.Delivered
                           || newStatus == ShipmentStatus.ReturnedToWarehouse
                           || newStatus == ShipmentStatus.Cancelled;
                    break;
                case ShipmentStatus.Delivered:
                    isValid = newStatus == ShipmentStatus.ReturnedToWarehouse; // kısmi iade sonrası
                    break;
                case ShipmentStatus.ReturnedToWarehouse:
                case ShipmentStatus.Cancelled:
                    isValid = false;
                    break;
                default:
                    isValid = false;
                    break;
            }

            if (!isValid)
            {
                throw new DomainException($"Invalid status transition from {Status} to {newStatus}");
            }
        }
    }
}
