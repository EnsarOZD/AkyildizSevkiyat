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
        string? NetsisStockCode
    );

    public record GetStocksQuery(string? SearchTerm, int PageNumber = 1, int PageSize = 15) : IRequest<PaginatedList<StockDto>>;

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
                        s.NetsisStockCode
                    )), 
                request.PageNumber, 
                request.PageSize
            );
        }
    }
}
