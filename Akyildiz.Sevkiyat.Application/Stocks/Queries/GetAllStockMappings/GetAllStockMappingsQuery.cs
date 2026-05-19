using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Common.Models;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Stocks.Queries.GetAllStockMappings
{
    public class StockMappingListDto
    {
        public int Id { get; set; }
        public string ExternalCode { get; set; } = "";
        public string ExternalName { get; set; } = "";
        public string Status { get; set; } = "";
        public int StatusValue { get; set; }
        public int? LocalStockId { get; set; }
        public string? LocalStockCode { get; set; }
        public string? LocalStockName { get; set; }
        public string? NetsisStockCode { get; set; }
    }

    public class GetAllStockMappingsQuery : IRequest<PaginatedList<StockMappingListDto>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting" };

        public string? StatusFilter { get; set; }
        public string? Search { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GetAllStockMappingsQueryHandler
        : IRequestHandler<GetAllStockMappingsQuery, PaginatedList<StockMappingListDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetAllStockMappingsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<StockMappingListDto>> Handle(
            GetAllStockMappingsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.StockMappings
                .Include(m => m.LocalStock)
                .Where(m => m.ExternalSystem == "ISS-IP")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
                Enum.TryParse<MatchStatus>(request.StatusFilter, out var status))
            {
                query = query.Where(m => m.MatchStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim().ToLower();
                query = query.Where(m =>
                    m.ExternalStockCode.ToLower().Contains(s) ||
                    m.ExternalStockName.ToLower().Contains(s) ||
                    (m.LocalStock != null && m.LocalStock.StockCode.ToLower().Contains(s)) ||
                    (m.LocalStock != null && m.LocalStock.StockName.ToLower().Contains(s)));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(m => m.MatchStatus)
                .ThenBy(m => m.ExternalStockName)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(m => new StockMappingListDto
                {
                    Id             = m.Id,
                    ExternalCode   = m.ExternalStockCode,
                    ExternalName   = m.ExternalStockName,
                    Status         = m.MatchStatus.ToString(),
                    StatusValue    = (int)m.MatchStatus,
                    LocalStockId   = m.LocalStockId,
                    LocalStockCode = m.LocalStock != null ? m.LocalStock.StockCode : null,
                    LocalStockName = m.LocalStock != null ? m.LocalStock.StockName : null,
                    NetsisStockCode = m.LocalStock != null ? m.LocalStock.NetsisStockCode : null,
                })
                .ToListAsync(cancellationToken);

            return new PaginatedList<StockMappingListDto>(items, total, request.PageNumber, request.PageSize);
        }
    }
}
