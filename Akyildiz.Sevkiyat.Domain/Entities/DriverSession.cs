using Akyildiz.Sevkiyat.Domain.Common;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Domain.Entities
{
    public class DriverSession : AuditableEntity
    {
        public Guid Id { get; private set; }
        public int DriverId { get; private set; }
        public int VehicleId { get; private set; }
        public int? ZonePreparationId { get; private set; }
        public DateTime StartTime { get; private set; }
        public double StartLatitude { get; private set; }
        public double StartLongitude { get; private set; }
        public DateTime? EndTime { get; private set; }
        public double? EndLatitude { get; private set; }
        public double? EndLongitude { get; private set; }
        public int? TotalDurationMinutes { get; private set; }
        public DriverSessionStatus Status { get; private set; }
        public string? DeviceFingerprint { get; private set; }
        public string? Notes { get; private set; }
        public string? ClosedByUserId { get; private set; }

        // Navigation
        public Driver Driver { get; private set; } = null!;
        public Vehicle Vehicle { get; private set; } = null!;
        public ZonePreparation? ZonePreparation { get; private set; }

        private DriverSession() { }

        public static DriverSession Create(
            int driverId, int vehicleId, int? zonePreparationId,
            double latitude, double longitude, string? deviceFingerprint)
        {
            return new DriverSession
            {
                Id = Guid.NewGuid(),
                DriverId = driverId,
                VehicleId = vehicleId,
                ZonePreparationId = zonePreparationId,
                StartTime = DateTime.UtcNow,
                StartLatitude = latitude,
                StartLongitude = longitude,
                Status = DriverSessionStatus.Open,
                DeviceFingerprint = deviceFingerprint
            };
        }

        public void Close(double latitude, double longitude)
        {
            if (Status != DriverSessionStatus.Open)
                throw new DomainException("Sadece açık session kapatılabilir.");
            EndTime = DateTime.UtcNow;
            EndLatitude = latitude;
            EndLongitude = longitude;
            TotalDurationMinutes = (int)(EndTime.Value - StartTime).TotalMinutes;
            Status = DriverSessionStatus.Closed;
        }

        public void ForceClose(string adminUserId, string? notes = null)
        {
            if (Status != DriverSessionStatus.Open)
                throw new DomainException("Sadece açık session zorla kapatılabilir.");
            EndTime = DateTime.UtcNow;
            TotalDurationMinutes = (int)(EndTime.Value - StartTime).TotalMinutes;
            Status = DriverSessionStatus.ForceClosed;
            ClosedByUserId = adminUserId;
            Notes = notes;
        }
    }
}
