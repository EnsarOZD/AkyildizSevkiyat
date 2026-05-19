using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.LogMissingItemsMail
{
    public record LogMissingItemsMailCommand(int ShipmentId) : IRequest;

    public class LogMissingItemsMailCommandHandler : IRequestHandler<LogMissingItemsMailCommand>
    {
        private readonly IApplicationDbContext _context;

        public LogMissingItemsMailCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(LogMissingItemsMailCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            shipment.MarkMissingItemsMailSent();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
