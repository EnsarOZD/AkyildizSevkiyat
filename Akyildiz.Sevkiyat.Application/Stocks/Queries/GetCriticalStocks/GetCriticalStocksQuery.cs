using MediatR;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Stocks.Queries.GetCriticalStocks
{
    public record GetCriticalStocksQuery : IRequest<List<CriticalStockDto>>;

    public class CriticalStockDto
    {
        public int StockMasterId { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal OnHand { get; set; }
        public decimal MinStockQty { get; set; }
        public decimal? ReorderPoint { get; set; }
    }

    public class GetCriticalStocksQueryHandler : IRequestHandler<GetCriticalStocksQuery, List<CriticalStockDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetCriticalStocksQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CriticalStockDto>> Handle(GetCriticalStocksQuery request, CancellationToken cancellationToken)
        {
            return await _context.StockMasters
                .AsNoTracking()
                .Where(s => s.IsActive && s.MinStockQty.HasValue && s.MinStockQty.Value > 0
                    && (s.OnHandQty - s.ReservedQty) < s.MinStockQty.Value)
                .OrderByDescending(s => s.MinStockQty!.Value - (s.OnHandQty - s.ReservedQty))
                .Take(20)
                .Select(s => new CriticalStockDto
                {
                    StockMasterId = s.Id,
                    StockCode = s.StockCode,
                    StockName = s.StockName,
                    Unit = s.Unit.ToString(),
                    OnHand = s.OnHandQty - s.ReservedQty,
                    MinStockQty = s.MinStockQty!.Value,
                    ReorderPoint = s.ReorderPoint,
                })
                .ToListAsync(cancellationToken);
        }
    }
}
