using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockCounts.Queries.GetStockCounts
{
    public record GetStockCountsQuery : IRequest<List<StockCountSummaryDto>>;

    public class StockCountSummaryDto
    {
        public int Id { get; set; }
        public DateTime CountDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public int TotalLines { get; set; }
        public int CountedLines { get; set; }
        public int AdjustedLines { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class GetStockCountsQueryHandler : IRequestHandler<GetStockCountsQuery, List<StockCountSummaryDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetStockCountsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<StockCountSummaryDto>> Handle(GetStockCountsQuery request, CancellationToken cancellationToken)
        {
            return await _context.StockCounts
                .Include(c => c.Lines)
                .OrderByDescending(c => c.CountDate)
                .Select(c => new StockCountSummaryDto
                {
                    Id = c.Id,
                    CountDate = c.CountDate,
                    Status = c.Status.ToString(),
                    Note = c.Note,
                    TotalLines = c.Lines.Count,
                    CountedLines = c.Lines.Count(l => l.ActualQty != null),
                    AdjustedLines = c.Lines.Count(l => l.ActualQty != null && l.ActualQty != l.ExpectedQty),
                    CreatedAt = c.CreatedAt,
                    CompletedAt = c.CompletedAt
                })
                .ToListAsync(cancellationToken);
        }
    }
}
