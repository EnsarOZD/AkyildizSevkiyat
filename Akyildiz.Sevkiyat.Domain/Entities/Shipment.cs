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
        public ICollection<ShipmentDeliveryPhoto> DeliveryPhotos { get; private set; } = new List<ShipmentDeliveryPhoto>();

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
        public string? DeliveryPhotoPath { get; private set; }
        public double? DeliveryLatitude { get; private set; }
        public double? DeliveryLongitude { get; private set; }

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

        // Missing-items mail tracking
        public DateTime? MissingItemsMailSentAt { get; private set; }
        public void MarkMissingItemsMailSent() => MissingItemsMailSentAt = DateTime.UtcNow;

        // Cargo dispatch
        public CargoProvider? CargoProvider { get; private set; }
        public string? CargoTrackingNumber { get; private set; }

        // Yurtici Kargo entegrasyon alanları
        public string? YkCargoKey { get; private set; }
        public string? YkInvoiceKey { get; private set; }
        public int? YkJobId { get; private set; }
        public string? YkBarcode { get; private set; }
        public string? YkOperationStatus { get; private set; }
        public string? YkOperationMessage { get; private set; }
        public string? YkErrorCode { get; private set; }
        public string? YkErrorMessage { get; private set; }
        public DateTime? YkLastQueryAt { get; private set; }

        // Freight (nakliye) dispatch
        public string? FreightCarrierName { get; private set; }
        public string? FreightCarrierPlate { get; private set; }
        public string? FreightCarrierPhone { get; private set; }

        // Operasyon tipi — sevkiyat oluşturulurken stok kategorilerine göre otomatik belirlenir
        public OperationType OperationType { get; set; } = OperationType.Catering;

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
            if (current == ShipmentStatus.Created && next == ShipmentStatus.ReadyForDispatch) return true; // Kıyafet: depo adımları atlanır
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
            if (Status != ShipmentStatus.Created && Status != ShipmentStatus.ReadyForDispatch)
                throw new DomainException("Sevkiyat düzenlenemez. Yalnızca 'Taslak' veya 'Sevke Hazır' durumundaki sevkiyatlar düzenlenebilir.");

            DeliveryDate = newDate;
            // Only clear zone link for draft shipments; ReadyForDispatch stays in its zone
            if (Status == ShipmentStatus.Created)
            {
                ZonePreparationId = null;
                ZonePreparation = null;
            }
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
        /// Netsis'te sipariş artık mevcut değilse aktarım bilgisini sıfırlar.
        /// Sevkiyat tekrar "Netsis'e Aktar" butonuyla gönderilebilir hale gelir.
        /// </summary>
        public void RevertNetsisTransfer()
        {
            NetsisTransferredAt = null;
            IrsaliyeNo          = null;
            IrsaliyeDate        = null;

            if (IssOrder != null)
            {
                IssOrder.NetsisOrderNumber = null;
                IssOrder.IsTransferred = false;
            }
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
            string? photoBase64, int? deliveredByUserId, string? deliveredByRole, string? overrideNote,
            string? photoPath = null, double? latitude = null, double? longitude = null)
        {
            DeliveredAt = deliveredAt;
            DeliveryRecipient = recipient;
            DeliveryNote = note;
            DeliveredByUserId = deliveredByUserId;
            DeliveredByRole = deliveredByRole;
            DeliveryOverrideNote = overrideNote;
            DeliveryLatitude = latitude;
            DeliveryLongitude = longitude;

            if (photoPath != null)
                DeliveryPhotoPath = photoPath;
            else
                DeliveryPhotoBase64 = photoBase64;
        }

        public void ClearDeliveryProof()
        {
            DeliveredAt          = null;
            DeliveryRecipient    = null;
            DeliveryNote         = null;
            DeliveryPhotoBase64  = null;
            DeliveryPhotoPath    = null;
            DeliveryLatitude     = null;
            DeliveryLongitude    = null;
            DeliveredByUserId    = null;
            DeliveredByRole      = null;
            DeliveryOverrideNote = null;
        }

        public void RecordVehicleReturn(DateTime returnedAt, string? returnNote)
        {
            ReturnedAt = returnedAt;
            ReturnNote = returnNote;
        }

        public void ClearVehicleAssignment()
        {
            AssignedDriverName   = null;
            AssignedPlateNumber  = null;
            AssignedDriverId     = null;
            ReturnedAt           = null;
            ReturnNote           = null;
        }

        public void RecordDispatch(DateTime dispatchedAt, string confirmedByName)
        {
            DispatchedAt = dispatchedAt;
            DispatchConfirmedByName = confirmedByName;
        }

        public void SetCargoDispatch(Domain.Enums.CargoProvider provider, string? trackingNumber)
        {
            CargoProvider = provider;
            CargoTrackingNumber = trackingNumber;
            DispatchedAt = DateTime.UtcNow;
        }

        public void SetYkCargoInfo(
            string cargoKey,
            string? invoiceKey,
            int? jobId,
            string? barcode,
            string? operationStatus,
            string? operationMessage,
            string? errorCode,
            string? errorMessage)
        {
            YkCargoKey          = cargoKey;
            YkInvoiceKey        = invoiceKey;
            YkJobId             = jobId;
            if (!string.IsNullOrWhiteSpace(barcode))
                YkBarcode = barcode;
            YkOperationStatus   = operationStatus;
            YkOperationMessage  = operationMessage;
            YkErrorCode         = errorCode;
            YkErrorMessage      = errorMessage;
        }

        public void UpdateYkStatus(string? statusCode, string? statusMessage, string? barcode = null)
        {
            YkOperationStatus  = statusCode;
            YkOperationMessage = statusMessage;
            YkLastQueryAt      = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(barcode) && string.IsNullOrWhiteSpace(YkBarcode))
                YkBarcode = barcode;
        }

        public void SetFreightDispatch(string carrierName, string? carrierPlate, string? carrierPhone = null)
        {
            FreightCarrierName  = carrierName;
            FreightCarrierPlate = carrierPlate;
            FreightCarrierPhone = carrierPhone;
            DispatchedAt        = DateTime.UtcNow;
        }

        private void ValidateTransition(ShipmentStatus newStatus)
        {
            bool isValid = false;

            switch (Status)
            {
                case ShipmentStatus.Created:
                    isValid = newStatus == ShipmentStatus.AssignedToWarehouse
                           || newStatus == ShipmentStatus.Picking         // Allow Skip
                           || newStatus == ShipmentStatus.ReadyForDispatch // Allow Skip (Kıyafet)
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
                    isValid = newStatus == ShipmentStatus.AssignedToVehicle
                           || newStatus == ShipmentStatus.Dispatched // Nakliye/Kargo için ara adımı atla
                           || newStatus == ShipmentStatus.Delivered  // Depo teslim — araç atamadan teslim
                           || newStatus == ShipmentStatus.Picking    // admin gıda hazırlık geri al
                           || newStatus == ShipmentStatus.Cancelled
                           || newStatus == ShipmentStatus.Created;   // admin taslağa çek
                    break;
                case ShipmentStatus.AssignedToVehicle:
                    isValid = newStatus == ShipmentStatus.Dispatched
                           || newStatus == ShipmentStatus.Delivered
                           || newStatus == ShipmentStatus.ReturnedToWarehouse
                           || newStatus == ShipmentStatus.Cancelled
                           || newStatus == ShipmentStatus.ReadyForDispatch; // admin araç geri al
                    break;
                case ShipmentStatus.Dispatched:
                    isValid = newStatus == ShipmentStatus.Delivered
                           || newStatus == ShipmentStatus.ReturnedToWarehouse
                           || newStatus == ShipmentStatus.Cancelled
                           || newStatus == ShipmentStatus.ReadyForDispatch // admin geri al
                           || newStatus == ShipmentStatus.Created;         // admin taslağa çek
                    break;
                case ShipmentStatus.Delivered:
                    isValid = newStatus == ShipmentStatus.ReturnedToWarehouse // kısmi iade sonrası
                           || newStatus == ShipmentStatus.Created             // admin sıfırlama
                           || newStatus == ShipmentStatus.ReadyForDispatch;   // admin teslim geri al
                    break;
                case ShipmentStatus.ReturnedToWarehouse:
                    isValid = newStatus == ShipmentStatus.Created          // admin sıfırlama
                           || newStatus == ShipmentStatus.ReadyForDispatch; // admin kurtarma
                    break;
                case ShipmentStatus.Cancelled:
                    isValid = newStatus == ShipmentStatus.Created; // admin sıfırlama
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
