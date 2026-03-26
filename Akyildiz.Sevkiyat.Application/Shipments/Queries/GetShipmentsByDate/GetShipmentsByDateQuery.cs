using Akyildiz.Sevkiyat.Application.Common.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Shipments.Queries.GetShipmentsByDate
{
    public sealed record GetShipmentsByDateQuery(DateTime? Date) : IRequest<List<ShipmentDto>>;

    public sealed class GetShipmentsByDateQueryHandler : IRequestHandler<GetShipmentsByDateQuery, List<ShipmentDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<GetShipmentsByDateQueryHandler> _logger;

        public GetShipmentsByDateQueryHandler(IApplicationDbContext context, ILogger<GetShipmentsByDateQueryHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ShipmentDto>> Handle(GetShipmentsByDateQuery request, CancellationToken cancellationToken)
        {
            var date = request.Date ?? DateTime.Today;

            if (!request.Date.HasValue)
            {
                _logger.LogWarning("GetShipmentsByDate çağrısında Date parametresi null. Bugün ({Today}) kullanılıyor.", DateTime.Today);
            }

            var targetDate = date.Date;

            var query = _context.Shipments
                .Include(s => s.Lines)
                .Where(s => s.DeliveryDate.Date == targetDate)
                .AsQueryable();

            var shipments = await query
                .OrderByDescending(s => s.DeliveryDate)
                .ToListAsync(cancellationToken);

            return shipments.Select(s => new ShipmentDto(
                Id: s.Id,
                ShipmentDate: s.DeliveryDate,
                OrderId: s.IssOrderId,
                Lines: s.Lines.Select(l => new ShipmentLineDto(
                    Id: l.Id,
                    OrderLineId: l.IssOrderLineId ?? 0,
                    DeliveredQty: l.DeliveredQty
                )).ToList()
            )).ToList();
        }
    }
}
