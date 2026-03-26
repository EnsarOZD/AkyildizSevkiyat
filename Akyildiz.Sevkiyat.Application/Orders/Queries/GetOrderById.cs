using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akyildiz.Sevkiyat.Application.Common.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Orders.Queries
{
    // Sorgu
    public record GetOrderByIdQuery(int Id) : IRequest<IssOrderDto?>;

    // Handler
    public class GetOrderByIdQueryHandler
        : IRequestHandler<GetOrderByIdQuery, IssOrderDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetOrderByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IssOrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var order = await _context.IssOrders
                .Include(o => o.Project)
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

            if (order == null)
                return null;

            return new IssOrderDto(
                order.Id,
                order.ExternalOrderNumber,
                order.OrderDate,
                order.DeliveryDate,
                order.ProjectId,
                order.Project.Code,
                order.Project.Name,
                order.Project.Region ?? string.Empty,
                order.Lines
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
            );
        }
    }
}
