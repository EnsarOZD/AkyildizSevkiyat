using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Driver.Commands.EndDriverSession
{
    public class EndDriverSessionCommandHandler
        : IRequestHandler<EndDriverSessionCommand, EndDriverSessionResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IPhotoStorageService _photos;

        public EndDriverSessionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IPhotoStorageService photos)
        {
            _context = context;
            _currentUser = currentUser;
            _photos = photos;
        }

        public async Task<EndDriverSessionResult> Handle(
            EndDriverSessionCommand command,
            CancellationToken cancellationToken)
        {
            // 1. JWT'den Driver bul
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId && d.IsActive, cancellationToken)
                ?? throw new NotFoundException("Şoför kaydı bulunamadı.");

            // 2. QR koddan araç bul
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.QrCode == command.QrCode, cancellationToken)
                ?? throw new DomainException("Geçersiz QR kodu.");

            // 3. Driver'ın açık session'ını bul
            var session = await _context.DriverSessions
                .FirstOrDefaultAsync(ds =>
                    ds.DriverId == driver.Id &&
                    ds.Status == DriverSessionStatus.Open, cancellationToken)
                ?? throw new DomainException("Açık seferiniz bulunmuyor.");

            // 4. Farklı araç QR'ı okutulmuş mu?
            if (session.VehicleId != vehicle.Id)
                throw new DomainException(
                    "Farklı araç QR'ı okuttunuz. Seferi başlattığınız aracın QR'ını okutun.");

            var today = DateTime.UtcNow.Date;

            // 5. Bitiş kadranı (km + foto): aynı araç için bugün başka bir şoför zaten
            //    bitiş kadranını girdiyse miras al, yoksa ilk kapatan şoförden zorunlu iste.
            var existingEndOdometerSession = await _context.DriverSessions
                .Where(ds =>
                    ds.VehicleId == vehicle.Id &&
                    ds.Status == DriverSessionStatus.Closed &&
                    ds.StartTime >= today &&
                    ds.EndOdometerKm != null)
                .OrderByDescending(ds => ds.EndTime)
                .FirstOrDefaultAsync(cancellationToken);

            string? odometerPath;
            int? endOdometerKm;
            if (!string.IsNullOrWhiteSpace(command.EndOdometerPhotoBase64) && command.EndOdometerKm is > 0)
            {
                odometerPath = await _photos.SaveAsync(command.EndOdometerPhotoBase64, "odometer", cancellationToken);
                endOdometerKm = command.EndOdometerKm;
            }
            else if (existingEndOdometerSession != null)
            {
                // Aynı araçta ilk kapatan şoför kadranı girmiş → kayıt tutarlılığı için miras al.
                odometerPath = existingEndOdometerSession.EndOdometerPhotoPath;
                endOdometerKm = existingEndOdometerSession.EndOdometerKm;
            }
            else
            {
                throw new DomainException("Bitiş kilometre ve kadran fotoğrafı zorunludur.");
            }

            // 6. Session'ı kapat
            session.Close(command.Latitude, command.Longitude, odometerPath, endOdometerKm);
            await _context.SaveChangesAsync(cancellationToken);

            return new EndDriverSessionResult(session.Id, session.TotalDurationMinutes ?? 0);
        }
    }
}
