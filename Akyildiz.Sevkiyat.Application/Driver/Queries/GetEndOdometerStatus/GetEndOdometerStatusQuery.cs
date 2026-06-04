using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Driver.Queries.GetEndOdometerStatus
{
    /// <summary>
    /// Sefer bitirmeden önce: bu araç için bugün başka bir şoför zaten bitiş kadranını
    /// (km + foto) girdi mi? Girdiyse şoföre kadran adımı sorulmaz. Salt-okunur.
    /// </summary>
    public record GetEndOdometerStatusQuery(string QrCode)
        : IRequest<EndOdometerStatusResult>;

    public record EndOdometerStatusResult(bool OdometerAlreadyRecorded);

    public class GetEndOdometerStatusQueryHandler
        : IRequestHandler<GetEndOdometerStatusQuery, EndOdometerStatusResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetEndOdometerStatusQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<EndOdometerStatusResult> Handle(
            GetEndOdometerStatusQuery request,
            CancellationToken cancellationToken)
        {
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId && d.IsActive, cancellationToken)
                ?? throw new NotFoundException("Şoför kaydı bulunamadı.");

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.QrCode == request.QrCode, cancellationToken)
                ?? throw new DomainException("Geçersiz araç QR kodu.");

            var today = DateTime.UtcNow.Date;

            // Bu araç için bugün başka bir şoför zaten bitiş kadranını girmiş mi?
            var alreadyRecorded = await _context.DriverSessions
                .AnyAsync(ds =>
                    ds.VehicleId == vehicle.Id &&
                    ds.Status == DriverSessionStatus.Closed &&
                    ds.StartTime >= today &&
                    ds.EndOdometerKm != null, cancellationToken);

            return new EndOdometerStatusResult(alreadyRecorded);
        }
    }
}
