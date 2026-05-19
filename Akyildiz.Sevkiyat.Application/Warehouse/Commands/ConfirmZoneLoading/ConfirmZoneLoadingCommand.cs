using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.ConfirmZoneLoading
{
    public record ConfirmZoneLoadingCommand(int ZonePreparationId) : IRequest<bool>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse", "Driver" };
    }

    public class ConfirmZoneLoadingCommandHandler : IRequestHandler<ConfirmZoneLoadingCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ConfirmZoneLoadingCommandHandler> _logger;

        public ConfirmZoneLoadingCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<ConfirmZoneLoadingCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<bool> Handle(ConfirmZoneLoadingCommand request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            if (zp.Status != ZonePreparationStatus.ReadyForTransfer)
                throw new DomainException("Yükleme onayı yalnızca 'Araca Atandı' aşamasındaki hazırlıklar için yapılabilir.");

            // Şoförün önceki seferi bitmeden yükleme onaylanamaz
            var primaryDriverId = await _context.ZonePreparationDrivers
                .Where(zpd => zpd.ZonePreparationId == zp.Id && zpd.IsPrimary)
                .Select(zpd => (int?)zpd.DriverId)
                .FirstOrDefaultAsync(cancellationToken);

            if (primaryDriverId.HasValue)
            {
                var openElsewhere = await _context.DriverSessions
                    .AnyAsync(ds =>
                        ds.DriverId == primaryDriverId.Value &&
                        ds.Status == DriverSessionStatus.Open &&
                        ds.ZonePreparationId != zp.Id, cancellationToken);

                if (openElsewhere)
                    throw new DomainException(
                        "Şoförün aktif seferi devam ediyor. Yükleme onaylanabilmesi için şoförün mevcut seferini kapatması gerekir.");
            }

            var shipments = await _context.WarehouseShipments
                .Where(s => s.ZonePreparationId == zp.Id
                         && s.Status == ShipmentStatus.AssignedToVehicle)
                .ToListAsync(cancellationToken);

            if (shipments.Count == 0)
                throw new DomainException("Yükleme onaylanacak sevkiyat bulunamadı.");

            var confirmedByName = "Sistem";
            if (_currentUserService.UserId.HasValue)
            {
                var user = await _context.Users
                    .Where(u => u.Id == _currentUserService.UserId.Value)
                    .Select(u => new { u.FirstName, u.LastName })
                    .FirstOrDefaultAsync(cancellationToken);
                if (user != null) confirmedByName = $"{user.FirstName} {user.LastName}".Trim();
            }

            var now = DateTime.UtcNow;

            foreach (var shipment in shipments)
            {
                shipment.ChangeStatus(ShipmentStatus.Dispatched, _currentUserService.UserId);
                shipment.RecordDispatch(now, confirmedByName);
            }

            zp.Status = ZonePreparationStatus.Dispatched;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
