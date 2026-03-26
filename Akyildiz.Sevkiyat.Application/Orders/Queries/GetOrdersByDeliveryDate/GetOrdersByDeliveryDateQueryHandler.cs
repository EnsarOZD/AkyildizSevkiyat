using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Akyildiz.Sevkiyat.Application.Common.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Orders.Queries.GetOrdersByDeliveryDate
{
    public class GetOrdersByDeliveryDateQueryHandler
        : IRequestHandler<GetOrdersByDeliveryDateQuery, List<IssOrderDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetOrdersByDeliveryDateQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<IssOrderDto>> Handle(
            GetOrdersByDeliveryDateQuery request,
            CancellationToken cancellationToken)
        {
            var date = request.DeliveryDate.Date;

            var orders = await _context.IssOrders
                .Include(o => o.Project)
                .Include(o => o.Lines)
                .Where(o => o.DeliveryDate.Date == date)
                .ToListAsync(cancellationToken);

            return orders.Select(o => new IssOrderDto(
                o.Id,
                o.ExternalOrderNumber,
                o.OrderDate,
                o.DeliveryDate,
                o.ProjectId,
                o.Project?.Code ?? string.Empty,
                o.Project?.Name ?? string.Empty,
                o.Project?.Region ?? string.Empty,
                o.Lines
                    .OrderBy(l => l.LineNumber)
                    .Select(l => new IssOrderLineDto(
                        l.Id,
                        l.LineNumber,
                        l.StockCode,
                        l.StockName,
                        l.Unit.ToString(),
                        l.OrderedQty
                    ))
                    .ToList()
            )).ToList();
        }
    }
}
