using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Reports.Queries.GetStockStatusReport
{
    public record GetStockStatusReportQuery(bool CriticalOnly = false) : IRequest<StockStatusReportDto>;

    public class StockStatusReportDto
    {
        public int TotalStocks { get; set; }
        public int CriticalCount { get; set; }
        public int OutOfStockCount { get; set; }
        public List<StockStatusRow> Rows { get; set; } = new();
    }

    public class StockStatusRow
    {
        public int Id { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? WarehouseLocation { get; set; }
        public decimal OnHandQty { get; set; }
        public decimal ReservedQty { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal? MinStockQty { get; set; }
        public bool IsCritical { get; set; }
        public bool IsOutOfStock { get; set; }
    }

    public class GetStockStatusReportQueryHandler
        : IRequestHandler<GetStockStatusReportQuery, StockStatusReportDto>
    {
        private readonly IApplicationDbContext _context;

        public GetStockStatusReportQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StockStatusReportDto> Handle(
            GetStockStatusReportQuery request, CancellationToken cancellationToken)
        {
            var query = _context.StockMasters.Where(s => s.IsActive);

            var stocks = await query
                .OrderBy(s => s.StockCode)
                .Select(s => new StockStatusRow
                {
                    Id                = s.Id,
                    StockCode         = s.StockCode,
                    StockName         = s.StockName,
                    Category          = s.Category.ToString(),
                    WarehouseLocation = s.WarehouseLocation,
                    OnHandQty         = s.OnHandQty,
                    ReservedQty       = s.ReservedQty,
                    AvailableQty      = s.OnHandQty - s.ReservedQty,
                    MinStockQty       = s.MinStockQty,
                    IsCritical        = s.MinStockQty.HasValue && (s.OnHandQty - s.ReservedQty) < s.MinStockQty.Value,
                    IsOutOfStock      = (s.OnHandQty - s.ReservedQty) <= 0,
                })
                .ToListAsync(cancellationToken);

            if (request.CriticalOnly)
                stocks = stocks.Where(s => s.IsCritical || s.IsOutOfStock).ToList();

            // Sort: out-of-stock first, then critical, then rest
            stocks = stocks
                .OrderByDescending(s => s.IsOutOfStock)
                .ThenByDescending(s => s.IsCritical)
                .ThenBy(s => s.StockCode)
                .ToList();

            return new StockStatusReportDto
            {
                TotalStocks    = stocks.Count,
                CriticalCount  = stocks.Count(s => s.IsCritical),
                OutOfStockCount = stocks.Count(s => s.IsOutOfStock),
                Rows           = stocks,
            };
        }
    }
}
