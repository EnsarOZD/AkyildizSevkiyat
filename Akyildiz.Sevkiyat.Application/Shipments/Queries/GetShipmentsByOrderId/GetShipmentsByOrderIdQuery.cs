using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Common.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Queries
{
    public record GetShipmentsByOrderIdQuery(int OrderId) : IRequest<List<ShipmentDto>>;

    public class GetShipmentsByOrderIdQueryHandler
        : IRequestHandler<GetShipmentsByOrderIdQuery, List<ShipmentDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetShipmentsByOrderIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ShipmentDto>> Handle(GetShipmentsByOrderIdQuery request, CancellationToken cancellationToken)
        {
            var shipments = await _context.Shipments
                .Include(s => s.Lines)
                .Where(s => s.IssOrderId == request.OrderId)
                .OrderBy(s => s.DeliveryDate)
                .ToListAsync(cancellationToken);

            return shipments.Select(s => new ShipmentDto(
                s.Id,
                s.DeliveryDate,
                s.IssOrderId,
                s.Lines.Select(l => new ShipmentLineDto(
                    l.Id,
                    l.IssOrderLineId ?? 0,
                    l.DeliveredQty
                )).ToList()
            )).ToList();
        }
    }
}
