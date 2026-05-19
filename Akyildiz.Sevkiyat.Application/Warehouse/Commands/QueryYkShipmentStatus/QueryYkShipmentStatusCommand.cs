using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.External.YurtiKargo;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.QueryYkShipmentStatus
{
    public record QueryYkShipmentStatusCommand : IRequest<YkShipmentStatus?>, IRequireRoles
    {
        public int ShipmentId { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse", "Dispatcher" };
    }

    public class QueryYkShipmentStatusCommandHandler : IRequestHandler<QueryYkShipmentStatusCommand, YkShipmentStatus?>
    {
        private readonly IApplicationDbContext _context;
        private readonly IYurtiKargoClient _ykClient;

        public QueryYkShipmentStatusCommandHandler(
            IApplicationDbContext context,
            IYurtiKargoClient ykClient)
        {
            _context  = context;
            _ykClient = ykClient;
        }

        public async Task<YkShipmentStatus?> Handle(QueryYkShipmentStatusCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (string.IsNullOrEmpty(shipment.YkCargoKey))
                throw new DomainException("Bu sevkiyat için Yurtici Kargo kaydı bulunamadı.");

            var status = await _ykClient.QueryShipmentAsync(shipment.YkCargoKey, cancellationToken);

            if (status != null)
            {
                shipment.UpdateYkStatus(status.StatusCode, status.StatusDescription, status.Barcode);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return status;
        }
    }
}
