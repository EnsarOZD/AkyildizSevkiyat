using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Common.Models;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Stocks.Queries.GetStocks
{
    public record StockDto(
        int Id,
        string StockCode,
        string StockName,
        string? Unit,
        decimal? UnitPrice,
        decimal? TaxRate,
        string PickingType,
        int PickingTypeId,
        string? Category,
        int CategoryId,
        int UnitId,
        string? Brand,
        decimal? MinStockQty,
        string? WarehouseLocation,
        decimal OnHandQty,
        decimal ReservedQty,
        string? NetsisStockCode,
        bool IsActive,
        decimal? WeightKg,
        int PickingOrder,
        string? Barcode
    );

    public record GetStocksQuery(string? SearchTerm, int PageNumber = 1, int PageSize = 15, int? CategoryId = null, int? PickingTypeId = null, int? UnitId = null, bool? IsActive = null, int? ExcludeCategoryId = null)
        : IRequest<PaginatedList<StockDto>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse", "Accounting" };
    }

    public class GetStocksQueryHandler : IRequestHandler<GetStocksQuery, PaginatedList<StockDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetStocksQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<StockDto>> Handle(GetStocksQuery request, CancellationToken cancellationToken)
        {
            var query = _context.StockMasters.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                query = query.Where(s =>
                    EF.Functions.Collate(s.StockCode, "Turkish_CI_AS").Contains(EF.Functions.Collate(term, "Turkish_CI_AS")) ||
                    EF.Functions.Collate(s.StockName, "Turkish_CI_AS").Contains(EF.Functions.Collate(term, "Turkish_CI_AS")));
            }

            if (request.CategoryId.HasValue)
                query = query.Where(s => (int)s.Category == request.CategoryId.Value);

            if (request.PickingTypeId.HasValue)
                query = query.Where(s => (int)s.PickingType == request.PickingTypeId.Value);

            if (request.UnitId.HasValue)
                query = query.Where(s => (int)s.Unit == request.UnitId.Value);

            if (request.IsActive.HasValue)
                query = query.Where(s => s.IsActive == request.IsActive.Value);

            if (request.ExcludeCategoryId.HasValue)
                query = query.Where(s => (int)s.Category != request.ExcludeCategoryId.Value);

            return await PaginatedList<StockDto>.CreateAsync(
                query
                    .OrderBy(s => s.StockCode)
                    .Select(s => new StockDto(
                        s.Id,
                        s.StockCode,
                        s.StockName,
                        s.Unit.ToString(),
                        s.UnitPrice,
                        (decimal)s.TaxRate,
                        s.PickingType.ToString(),
                        (int)s.PickingType,
                        s.Category.ToString(),
                        (int)s.Category,
                        (int)s.Unit,
                        s.Brand,
                        s.MinStockQty,
                        s.WarehouseLocation,
                        s.OnHandQty,
                        s.ReservedQty,
                        s.NetsisStockCode,
                        s.IsActive,
                        s.WeightKg,
                        s.PickingOrder,
                        s.Barcode
                    )),
                request.PageNumber,
                request.PageSize
            );
        }
    }
}
