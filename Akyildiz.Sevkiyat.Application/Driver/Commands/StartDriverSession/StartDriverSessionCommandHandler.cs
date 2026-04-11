using MediatR;
using Microsoft.EntityFrameworkCore;
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

        public StartDriverSessionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
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

            // 4. Bu driver bu araca atanmış mı? (aktif ZonePreparation'larda)
            var today = DateTime.UtcNow.Date;
            var assignment = await _context.ZonePreparationDrivers
                .Include(zpd => zpd.ZonePreparation)
                .FirstOrDefaultAsync(zpd =>
                    zpd.DriverId == driver.Id &&
                    zpd.ZonePreparation.VehicleId == vehicle.Id &&
                    zpd.ZonePreparation.DeliveryDate.Date >= today &&
                    zpd.ZonePreparation.Status <= ZonePreparationStatus.Dispatched,
                    cancellationToken);

            if (assignment == null)
                throw new DomainException("Bu araca atamanız bulunmuyor. Yöneticinizle iletişime geçin.");

            // 5. Session oluştur
            var session = DriverSession.Create(
                driver.Id,
                vehicle.Id,
                assignment.ZonePreparationId,
                command.Latitude,
                command.Longitude,
                command.DeviceFingerprint);

            _context.DriverSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            return new StartDriverSessionResult(session.Id, vehicle.PlateNumber, session.StartTime);
        }
    }
}
