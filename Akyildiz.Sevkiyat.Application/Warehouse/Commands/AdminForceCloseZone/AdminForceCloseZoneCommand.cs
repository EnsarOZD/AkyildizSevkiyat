using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.AdminForceCloseZone
{
    public record AdminForceCloseZoneCommand(int ZonePreparationId) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin" };
    }

    public class AdminForceCloseZoneCommandHandler : IRequestHandler<AdminForceCloseZoneCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AdminForceCloseZoneCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(AdminForceCloseZoneCommand request, CancellationToken cancellationToken)
        {
            var zp = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.Id == request.ZonePreparationId, cancellationToken)
                ?? throw new NotFoundException("ZonePreparation", request.ZonePreparationId);

            var userId = _currentUserService.UserId;
            const string reason = "Admin tarafından zona kapatıldı — sevke hazır durumuna geri alındı.";

            // Tüm bağlı sevkiyatları bul (teslim edilmiş/iptal olanlar hariç)
            var shipments = await _context.WarehouseShipments
                .Where(s =>
                    s.ZonePreparationId == request.ZonePreparationId &&
                    s.Status != ShipmentStatus.Delivered &&
                    s.Status != ShipmentStatus.Cancelled &&
                    s.Status != ShipmentStatus.ReturnedToWarehouse)
                .ToListAsync(cancellationToken);

            foreach (var s in shipments)
            {
                // ReadyForDispatch'te değilse geri al
                if (s.Status != ShipmentStatus.ReadyForDispatch)
                    s.ChangeStatus(ShipmentStatus.ReadyForDispatch, userId, reason);

                // Araç/sürücü atamasını temizle
                s.ClearVehicleAssignment();

                // Zonadan ayır
                s.ZonePreparationId = null;
            }

            zp.Status = ZonePreparationStatus.Dispatched;

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
