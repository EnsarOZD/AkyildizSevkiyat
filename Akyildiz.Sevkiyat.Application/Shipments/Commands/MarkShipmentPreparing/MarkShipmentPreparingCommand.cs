using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkShipmentPreparing
{
    public sealed record MarkShipmentPreparingCommand(int ShipmentId) : IRequest;

    public sealed class MarkShipmentPreparingCommandHandler
        : IRequestHandler<MarkShipmentPreparingCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public MarkShipmentPreparingCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(MarkShipmentPreparingCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken);

            if (shipment is null)
                throw new NotFoundException("Shipment", request.ShipmentId);

            shipment.ChangeStatus(Akyildiz.Sevkiyat.Domain.Enums.ShipmentStatus.AssignedToWarehouse, _currentUserService.UserId);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
