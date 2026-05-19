using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.StartPicking
{
    public class StartPickingCommandHandler : IRequestHandler<StartPickingCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StartPickingCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(StartPickingCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, cancellationToken);

            if (shipment == null)
            {
                throw new NotFoundException("Shipment", request.ShipmentId);
            }

            shipment.ChangeStatus(ShipmentStatus.Picking, _currentUserService.UserId, request.Reason);

            // Sevkiyat bir zone preparation'a bağlıysa ve zone hâlâ Draft'taysa,
            // zone'u da otomatik başlat — bireysel picking ile zone flow'u senkronize tutar.
            if (shipment.ZonePreparationId.HasValue)
            {
                var zone = await _context.ZonePreparations
                    .FirstOrDefaultAsync(z => z.Id == shipment.ZonePreparationId.Value, cancellationToken);

                if (zone != null && zone.Status == ZonePreparationStatus.Draft)
                {
                    zone.Status = ZonePreparationStatus.MicroPicking;
                    zone.IsFrozen = true;
                    zone.StartedAt = DateTime.UtcNow;
                    if (_currentUserService.UserId.HasValue)
                        zone.StartedByUserId = _currentUserService.UserId.Value;

                    // Zone'daki diğer AssignedToWarehouse sevkiyatları da Picking'e al
                    var siblingShipments = await _context.Shipments
                        .Where(s => s.ZonePreparationId == zone.Id
                                    && s.Id != shipment.Id
                                    && s.Status == ShipmentStatus.AssignedToWarehouse)
                        .ToListAsync(cancellationToken);

                    foreach (var s in siblingShipments)
                        s.ChangeStatus(ShipmentStatus.Picking, _currentUserService.UserId);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
