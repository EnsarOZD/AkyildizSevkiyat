using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.UpdateShipmentLineDeliveredQty
{
    public sealed record UpdateShipmentLineDeliveredQtyCommand(
        int ShipmentId,
        int LineId,
        decimal DeliveredQty,
        string? DifferenceReason,
        string? Note
    ) : IRequest;

    public sealed class UpdateShipmentLineDeliveredQtyCommandHandler
        : IRequestHandler<UpdateShipmentLineDeliveredQtyCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateShipmentLineDeliveredQtyCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateShipmentLineDeliveredQtyCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken);

            if (shipment is null)
                throw new DomainException($"Shipment not found. Id={request.ShipmentId}");

            if (shipment.Status == ShipmentStatus.Delivered)
                throw new DomainException("Delivered shipment lines cannot be updated.");

            var line = shipment.Lines.FirstOrDefault(l => l.Id == request.LineId);

            if (line is null)
                throw new DomainException($"ShipmentLine not found. Id={request.LineId}");

            line.SetDeliveredQty(request.DeliveredQty, request.DifferenceReason, request.Note);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
