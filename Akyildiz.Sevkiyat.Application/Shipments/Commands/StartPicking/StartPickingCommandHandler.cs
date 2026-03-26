using Akyildiz.Sevkiyat.Application.Interfaces;
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

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
