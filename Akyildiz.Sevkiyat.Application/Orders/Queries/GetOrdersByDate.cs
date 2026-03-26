using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akyildiz.Sevkiyat.Application.Common.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Orders.Queries
{
    // Sipariş tarihi bazlı liste
    public record GetOrdersByDateQuery(DateTime OrderDate) : IRequest<List<IssOrderDto>>;

    public class GetOrdersByDateQueryHandler
        : IRequestHandler<GetOrdersByDateQuery, List<IssOrderDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetOrdersByDateQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<IssOrderDto>> Handle(GetOrdersByDateQuery request, CancellationToken cancellationToken)
        {
            var targetDate = request.OrderDate.Date;

            var orders = await _context.IssOrders
                .Include(o => o.Project)
                .Include(o => o.Lines)
                .Where(o => o.OrderDate.Date == targetDate)
                .OrderBy(o => o.DeliveryDate)
                .ThenBy(o => o.Id)
                .ToListAsync(cancellationToken);

            return orders.Select(o => new IssOrderDto(
                o.Id,
                o.ExternalOrderNumber,
                o.OrderDate,
                o.DeliveryDate,
                o.ProjectId,
                o.Project.Code,
                o.Project.Name,
                o.Project.Region ?? string.Empty,
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
