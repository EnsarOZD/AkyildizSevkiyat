using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Common.Services;
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

            // 3b. Aynı araç için bugün başka bir şoför zaten kadran (km + foto) girdiyse,
            //     bu şoföre tekrar sorma — ilk okuma tüm seferi temsil eder. Araca birden
            //     fazla şoför atandığında her şoför kendi seferini başlatır ama kadranı
            //     yalnızca ilk başlatan girer.
            var existingOdometerSession = await _context.DriverSessions
                .Where(ds =>
                    ds.VehicleId == vehicle.Id &&
                    ds.Status == DriverSessionStatus.Open &&
                    ds.StartTime >= today &&
                    ds.StartOdometerKm != null)
                .OrderBy(ds => ds.StartTime)
                .FirstOrDefaultAsync(cancellationToken);

            var odometerAlreadyRecorded = existingOdometerSession != null;

            // 4a. Depo akışı: ZonePreparationDrivers — bu araç+şoföre atanmış TÜM aktif zone'lar.
            //     Bir araca aynı gün birden fazla zone atanabilir; hepsini kapsa.
            var zoneIds = await _context.ZonePreparationDrivers
                .Where(zpd =>
                    zpd.DriverId == driver.Id &&
                    zpd.ZonePreparation.VehicleId == vehicle.Id &&
                    zpd.ZonePreparation.DeliveryDate.Date >= today &&
                    zpd.ZonePreparation.Status <= ZonePreparationStatus.Dispatched)
                .Select(zpd => zpd.ZonePreparationId)
                .Distinct()
                .ToListAsync(cancellationToken);

            // 4b. Direkt atama: Sevkiyatlar sayfasından AssignVehicle ile atanmış mı?
            // Sevkiyatlar ekranından atanan sevkiyatlar zaten Dispatched olarak gelir (zone onayı yok).
            bool hasDirectAssignment = false;
            if (zoneIds.Count == 0)
            {
                hasDirectAssignment = await _context.Shipments
                    .AnyAsync(s =>
                        s.AssignedDriverId == driver.Id &&
                        s.AssignedPlateNumber == vehicle.PlateNumber &&
                        (s.Status == ShipmentStatus.AssignedToVehicle ||
                         s.Status == ShipmentStatus.Dispatched),
                        cancellationToken);
            }

            if (zoneIds.Count == 0 && !hasDirectAssignment)
                throw new DomainException("Bu araca atamanız bulunmuyor. Yöneticinizle iletişime geçin.");

            // 4c. Bu seferin sevkiyatları (manifest adayı): tüm atanmış zone'lar veya direkt atama.
            var tripShipments = zoneIds.Count > 0
                ? await _context.Shipments
                    .Where(s =>
                        s.ZonePreparationId != null &&
                        zoneIds.Contains(s.ZonePreparationId.Value) &&
                        (s.Status == ShipmentStatus.AssignedToVehicle || s.Status == ShipmentStatus.Dispatched))
                    .ToListAsync(cancellationToken)
                : await _context.Shipments
                    .Where(s =>
                        s.AssignedDriverId == driver.Id &&
                        s.AssignedPlateNumber == vehicle.PlateNumber &&
                        (s.Status == ShipmentStatus.AssignedToVehicle || s.Status == ShipmentStatus.Dispatched))
                    .ToListAsync(cancellationToken);

            // 4d. Okutulan irsaliyenin bu sefere ait olduğunu doğrula (yanlış yük kontrolü).
            if (string.IsNullOrEmpty(IrsaliyeMatcher.Normalize(command.IrsaliyeNo)))
                throw new DomainException("İrsaliye numarası okunamadı. Lütfen irsaliye QR'ını tekrar okutun.");

            if (!tripShipments.Any(s => IrsaliyeMatcher.Matches(s.IrsaliyeNo, command.IrsaliyeNo)))
                throw new DomainException(
                    "Okuttuğunuz irsaliye bu araca atanmış sevkiyatlarla eşleşmiyor. " +
                    "Doğru aracın/seferin irsaliyesini okuttuğunuzdan emin olun.");

            // 5. Kadran (km + foto): ilk şoför için zorunlu, sonraki şoförler için ilk
            //    okumadan miras alınır.
            string? odometerPath;
            int? odometerKm;
            if (!string.IsNullOrWhiteSpace(command.StartOdometerPhotoBase64) && command.StartOdometerKm is > 0)
            {
                odometerPath = await _photos.SaveAsync(command.StartOdometerPhotoBase64, "odometer", cancellationToken);
                odometerKm = command.StartOdometerKm;
            }
            else if (odometerAlreadyRecorded)
            {
                // Aynı araçta ilk şoför kadranı girmiş → kayıt tutarlılığı için miras al.
                odometerPath = existingOdometerSession!.StartOdometerPhotoPath;
                odometerKm = existingOdometerSession.StartOdometerKm;
            }
            else
            {
                throw new DomainException("Başlangıç kilometre ve kadran fotoğrafı zorunludur.");
            }

            // 6. Session oluştur — birincil zone (varsa ilk zone); manifest tüm zone'ları kapsar.
            //    Zone'suz direkt atamada ZonePreparationId = null.
            var session = DriverSession.Create(
                driver.Id,
                vehicle.Id,
                zoneIds.Count > 0 ? zoneIds.First() : (int?)null,
                command.Latitude,
                command.Longitude,
                command.DeviceFingerprint,
                odometerPath,
                odometerKm);

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
    }
}
