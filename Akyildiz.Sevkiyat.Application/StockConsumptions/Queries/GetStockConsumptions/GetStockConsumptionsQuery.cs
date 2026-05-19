using Akyildiz.Sevkiyat.Application.Common.Models;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockConsumptions.Queries.GetStockConsumptions
{
    public class GetStockConsumptionsQuery : IRequest<PaginatedList<StockConsumptionDto>>
    {
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public StockConsumptionType? Type { get; set; }
        public string? Search { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 30;
    }

    public class StockConsumptionDto
    {
        public int Id { get; set; }
        public int StockMasterId { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int TypeValue { get; set; }
        public string TypeLabel { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public DateOnly Date { get; set; }
        public string? Reason { get; set; }
        public string? RecipientName { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal? TotalSaleAmount { get; set; }
        public string? Note { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GetStockConsumptionsQueryHandler : IRequestHandler<GetStockConsumptionsQuery, PaginatedList<StockConsumptionDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetStockConsumptionsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<StockConsumptionDto>> Handle(GetStockConsumptionsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.StockConsumptions.AsNoTracking();

            if (request.FromDate.HasValue)
                query = query.Where(x => x.Date >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                query = query.Where(x => x.Date <= request.ToDate.Value);
            if (request.Type.HasValue)
                query = query.Where(x => x.Type == request.Type.Value);
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                query = query.Where(x =>
                    x.StockCodeSnapshot.Contains(s) ||
                    x.StockNameSnapshot.Contains(s) ||
                    (x.RecipientName != null && x.RecipientName.Contains(s)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new StockConsumptionDto
                {
                    Id = x.Id,
                    StockMasterId = x.StockMasterId,
                    StockCode = x.StockCodeSnapshot,
                    StockName = x.StockNameSnapshot,
                    Unit = x.UnitSnapshot.ToString(),
                    TypeValue = (int)x.Type,
                    TypeLabel = x.Type == StockConsumptionType.Zai ? "Zai"
                              : x.Type == StockConsumptionType.DahiliKullanim ? "Dahili Kullanım"
                              : "Depo Satışı",
                    Quantity = x.Quantity,
                    Date = x.Date,
                    Reason = x.Reason,
                    RecipientName = x.RecipientName,
                    SalePrice = x.SalePrice,
                    TotalSaleAmount = x.SalePrice.HasValue ? x.SalePrice.Value * x.Quantity : null,
                    Note = x.Note,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new PaginatedList<StockConsumptionDto>(items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
