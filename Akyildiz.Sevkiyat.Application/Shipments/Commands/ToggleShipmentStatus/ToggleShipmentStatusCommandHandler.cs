using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Warehouse.Services;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.ToggleShipmentStatus
{
    public class ToggleShipmentStatusCommandHandler : IRequestHandler<ToggleShipmentStatusCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ZoneAutoCloseService _zoneAutoClose;

        public ToggleShipmentStatusCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ZoneAutoCloseService zoneAutoClose)
        {
            _context = context;
            _currentUserService = currentUserService;
            _zoneAutoClose = zoneAutoClose;
        }

        public async Task<Unit> Handle(ToggleShipmentStatusCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (request.SetPassive)
            {
                var zoneId = shipment.ZonePreparationId;
                shipment.SetPassive(_currentUserService.UserId);
                // Passive shipments should not block a zone — detach and recalculate
                shipment.ZonePreparationId = null;

                await _context.SaveChangesAsync(cancellationToken);

                if (zoneId.HasValue)
                    await _zoneAutoClose.TryAutoCloseAsync(zoneId.Value, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                shipment.SetActive(_currentUserService.UserId, request.Reason);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Unit.Value;
        }
    }
}
