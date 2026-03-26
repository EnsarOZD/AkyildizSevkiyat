using Akyildiz.Sevkiyat.Application.Interfaces;
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

        public ToggleShipmentStatusCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(ToggleShipmentStatusCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken);
            
            if (shipment == null)
                throw new NotFoundException("Shipment", request.ShipmentId);

            // 2. İşlem
            if (request.SetPassive)
            {
                shipment.SetPassive(_currentUserService.UserId);
            }
            else
            {
                shipment.SetActive(_currentUserService.UserId, request.Reason);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
