using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Driver.Commands.StartDriverSession
{
    public class StartDriverSessionCommandHandler
        : IRequestHandler<StartDriverSessionCommand, StartDriverSessionResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IPhotoStorageService _photos;

        public StartDriverSessionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IPhotoStorageService photos)
        {
            _context = context;
            _currentUser = currentUser;
            _photos = photos;
        }

        public async Task<StartDriverSessionResult> Handle(
            StartDriverSessionCommand command,
            CancellationToken cancellationToken)
        {
            // 1. JWT'den UserId → Driver bul
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId && d.IsActive, cancellationToken)
                ?? throw new NotFoundException("Şoför kaydı bulunamadı.");

            // 2. QR koddan araç bul
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.QrCode == command.QrCode, cancellationToken)
                ?? throw new DomainException("Geçersiz QR kodu.");

            if (!vehicle.IsActive)
                throw new DomainException("Araç aktif değil.");

            // 3. Bu driver'ın zaten açık session'ı var mı?
            var hasOpenSession = await _context.DriverSessions
                .AnyAsync(ds => ds.DriverId == driver.Id && ds.Status == DriverSessionStatus.Open, cancellationToken);

            if (hasOpenSession)
                throw new DomainException("Zaten açık seferiniz var. Önce mevcut seferi kapatın.");

            var today = DateTime.UtcNow.Date;

            // 4a. Depo akışı: ZonePreparationDrivers tablosunda atama var mı?
            var zoneAssignment = await _context.ZonePreparationDrivers
                .Include(zpd => zpd.ZonePreparation)
                .FirstOrDefaultAsync(zpd =>
                    zpd.DriverId == driver.Id &&
                    zpd.ZonePreparation.VehicleId == vehicle.Id &&
                    zpd.ZonePreparation.DeliveryDate.Date >= today &&
                    zpd.ZonePreparation.Status <= ZonePreparationStatus.Dispatched,
                    cancellationToken);

            // 4b. Direkt atama: Sevkiyatlar sayfasından AssignVehicle ile atanmış mı?
            // Sevkiyatlar ekranından atanan sevkiyatlar zaten Dispatched olarak gelir (zone onayı yok).
            bool hasDirectAssignment = false;
            if (zoneAssignment == null)
            {
                hasDirectAssignment = await _context.Shipments
                    .AnyAsync(s =>
                        s.AssignedDriverId == driver.Id &&
                        s.AssignedPlateNumber == vehicle.PlateNumber &&
                        (s.Status == ShipmentStatus.AssignedToVehicle ||
                         s.Status == ShipmentStatus.Dispatched),
                        cancellationToken);
            }

            if (zoneAssignment == null && !hasDirectAssignment)
                throw new DomainException("Bu araca atamanız bulunmuyor. Yöneticinizle iletişime geçin.");

            // 4c. Bu seferin sevkiyatları (manifest adayı): zone bazlı veya direkt atama.
            var tripShipments = zoneAssignment != null
                ? await _context.Shipments
                    .Where(s =>
                        s.ZonePreparationId == zoneAssignment.ZonePreparationId &&
                        (s.Status == ShipmentStatus.AssignedToVehicle || s.Status == ShipmentStatus.Dispatched))
                    .ToListAsync(cancellationToken)
                : await _context.Shipments
                    .Where(s =>
                        s.AssignedDriverId == driver.Id &&
                        s.AssignedPlateNumber == vehicle.PlateNumber &&
                        (s.Status == ShipmentStatus.AssignedToVehicle || s.Status == ShipmentStatus.Dispatched))
                    .ToListAsync(cancellationToken);

            // 4d. Okutulan irsaliyenin bu sefere ait olduğunu doğrula (yanlış yük kontrolü).
            var scannedIrsaliye = NormalizeIrsaliye(command.IrsaliyeNo);
            if (string.IsNullOrEmpty(scannedIrsaliye))
                throw new DomainException("İrsaliye numarası okunamadı. Lütfen irsaliye QR'ını tekrar okutun.");

            if (!tripShipments.Any(s => NormalizeIrsaliye(s.IrsaliyeNo) == scannedIrsaliye))
                throw new DomainException(
                    "Okuttuğunuz irsaliye bu araca atanmış sevkiyatlarla eşleşmiyor. " +
                    "Doğru aracın/seferin irsaliyesini okuttuğunuzdan emin olun.");

            // 5. Kadran fotoğrafı varsa kaydet
            string? odometerPath = null;
            if (!string.IsNullOrWhiteSpace(command.StartOdometerPhotoBase64))
                odometerPath = await _photos.SaveAsync(command.StartOdometerPhotoBase64, "odometer", cancellationToken);

            // 6. Session oluştur (zone'suz direkt atamada ZonePreparationId = null)
            var session = DriverSession.Create(
                driver.Id,
                vehicle.Id,
                zoneAssignment?.ZonePreparationId,
                command.Latitude,
                command.Longitude,
                command.DeviceFingerprint,
                odometerPath,
                command.StartOdometerKm);

            _context.DriverSessions.Add(session);

            // 7. AssignedToVehicle sevkiyatları Dispatched'e al (yolda)
            foreach (var s in tripShipments.Where(s => s.Status == ShipmentStatus.AssignedToVehicle))
                s.ChangeStatus(ShipmentStatus.Dispatched, driver.UserId);

            // 8. Sefer-sevkiyat manifestini yaz (geçmişe dönük "bu seferde bu sevkiyatlar vardı")
            foreach (var s in tripShipments)
                _context.DriverSessionShipments.Add(DriverSessionShipment.Create(session.Id, s.Id));

            await _context.SaveChangesAsync(cancellationToken);

            return new StartDriverSessionResult(session.Id, vehicle.PlateNumber, session.StartTime, tripShipments.Count);
        }

        /// <summary>İrsaliye no karşılaştırması için normalize: harf/rakam dışını at, büyük harfe çevir.</summary>
        private static string NormalizeIrsaliye(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return new string(raw.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }
    }
}
