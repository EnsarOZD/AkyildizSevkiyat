using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Driver.Queries.GetActiveDriverSession
{
    public class GetActiveDriverSessionQueryHandler
        : IRequestHandler<GetActiveDriverSessionQuery, ActiveDriverSessionDto?>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetActiveDriverSessionQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<ActiveDriverSessionDto?> Handle(
            GetActiveDriverSessionQuery request,
            CancellationToken cancellationToken)
        {
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId && d.IsActive, cancellationToken)
                ?? throw new NotFoundException("Şoför kaydı bulunamadı.");

            var session = await _context.DriverSessions
                .Include(ds => ds.Vehicle)
                .FirstOrDefaultAsync(ds =>
                    ds.DriverId == driver.Id &&
                    ds.Status == DriverSessionStatus.Open, cancellationToken);

            if (session == null) return null;

            var elapsed = (int)(DateTime.UtcNow - session.StartTime).TotalMinutes;

            return new ActiveDriverSessionDto(
                session.Id,
                session.VehicleId,
                session.Vehicle.PlateNumber,
                session.StartTime,
                session.StartLatitude,
                session.StartLongitude,
                elapsed);
        }
    }
}
