using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Search.Queries.GlobalSearch
{
    public record GetGlobalSearchQuery(string Q) : IRequest<GlobalSearchResultDto>;

    public class GlobalSearchResultDto
    {
        public List<SearchShipmentDto> Shipments { get; set; } = new();
        public List<SearchStockDto>    Stocks    { get; set; } = new();
        public List<SearchProjectDto>  Projects  { get; set; } = new();
    }

    public class SearchShipmentDto
    {
        public int      Id           { get; set; }
        public string?  TalepNo      { get; set; }
        public string   ProjectName  { get; set; } = string.Empty;
        public string   ProjectCode  { get; set; } = string.Empty;
        public string   Status       { get; set; } = string.Empty;
        public DateTime DeliveryDate { get; set; }
    }

    public class SearchStockDto
    {
        public int     Id           { get; set; }
        public string  StockCode    { get; set; } = string.Empty;
        public string  StockName    { get; set; } = string.Empty;
        public decimal AvailableQty { get; set; }
    }

    public class SearchProjectDto
    {
        public int     Id     { get; set; }
        public string  Code   { get; set; } = string.Empty;
        public string  Name   { get; set; } = string.Empty;
        public string? Region { get; set; }
    }

    public class GetGlobalSearchQueryHandler
        : IRequestHandler<GetGlobalSearchQuery, GlobalSearchResultDto>
    {
        private readonly IApplicationDbContext _context;

        public GetGlobalSearchQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GlobalSearchResultDto> Handle(
            GetGlobalSearchQuery request,
            CancellationToken cancellationToken)
        {
            var q = request.Q.Trim();

            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return new GlobalSearchResultDto();

            var result = new GlobalSearchResultDto();

            // ── Shipments ───────────────────────────────────────────────────
            // Search by numeric ID, TalepNo, project name/code
            bool isNumeric = int.TryParse(q, out int shipId);

            result.Shipments = await _context.Shipments
                .Include(s => s.Project)
                .Where(s =>
                    (isNumeric && s.Id == shipId) ||
                    (s.TalepNo   != null && EF.Functions.Collate(s.TalepNo, "Turkish_CI_AS").Contains(EF.Functions.Collate(q, "Turkish_CI_AS"))) ||
                    EF.Functions.Collate(s.Project.Name, "Turkish_CI_AS").Contains(EF.Functions.Collate(q, "Turkish_CI_AS")) ||
                    EF.Functions.Collate(s.Project.Code, "Turkish_CI_AS").Contains(EF.Functions.Collate(q, "Turkish_CI_AS")))
                .OrderByDescending(s => s.Id)
                .Take(5)
                .Select(s => new SearchShipmentDto
                {
                    Id           = s.Id,
                    TalepNo      = s.TalepNo,
                    ProjectName  = s.Project.Name,
                    ProjectCode  = s.Project.Code,
                    Status       = s.Status.ToString(),
                    DeliveryDate = s.DeliveryDate,
                })
                .ToListAsync(cancellationToken);

            // ── Stocks ──────────────────────────────────────────────────────
            result.Stocks = await _context.StockMasters
                .Where(s => s.IsActive && (
                    EF.Functions.Collate(s.StockCode, "Turkish_CI_AS").Contains(EF.Functions.Collate(q, "Turkish_CI_AS")) ||
                    EF.Functions.Collate(s.StockName, "Turkish_CI_AS").Contains(EF.Functions.Collate(q, "Turkish_CI_AS"))))
                .OrderBy(s => s.StockCode)
                .Take(5)
                .Select(s => new SearchStockDto
                {
                    Id           = s.Id,
                    StockCode    = s.StockCode,
                    StockName    = s.StockName,
                    AvailableQty = s.OnHandQty - s.ReservedQty,
                })
                .ToListAsync(cancellationToken);

            // ── Projects ─────────────────────────────────────────────────────
            result.Projects = await _context.Projects
                .Where(p => p.IsActive && (
                    EF.Functions.Collate(p.Name, "Turkish_CI_AS").Contains(EF.Functions.Collate(q, "Turkish_CI_AS")) ||
                    EF.Functions.Collate(p.Code, "Turkish_CI_AS").Contains(EF.Functions.Collate(q, "Turkish_CI_AS"))))
                .OrderBy(p => p.Name)
                .Take(5)
                .Select(p => new SearchProjectDto
                {
                    Id     = p.Id,
                    Code   = p.Code,
                    Name   = p.Name,
                    Region = p.Region,
                })
                .ToListAsync(cancellationToken);

            return result;
        }
    }
}
