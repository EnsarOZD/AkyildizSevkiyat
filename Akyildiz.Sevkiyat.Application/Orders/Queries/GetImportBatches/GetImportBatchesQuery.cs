using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Orders.Queries.GetImportBatches
{
    public record GetImportBatchesQuery(int Page = 1, int PageSize = 20) : IRequest<GetImportBatchesResult>;

    public class GetImportBatchesResult
    {
        public List<ImportBatchDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class ImportBatchDto
    {
        public int Id { get; set; }
        public DateTime RequestedStartDate { get; set; }
        public DateTime RequestedEndDate { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = null!;
        public int TotalFromSource { get; set; }
        public int NewCount { get; set; }
        public int SkippedCount { get; set; }
        public int NeedsMappingCount { get; set; }
        public int FailedCount { get; set; }
        public int DurationMs { get; set; }
        public string? ErrorSummary { get; set; }
    }

    public class GetImportBatchesQueryHandler : IRequestHandler<GetImportBatchesQuery, GetImportBatchesResult>
    {
        private readonly IApplicationDbContext _context;

        public GetImportBatchesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GetImportBatchesResult> Handle(GetImportBatchesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.ImportBatches.OrderByDescending(b => b.StartedAt);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(b => new ImportBatchDto
                {
                    Id = b.Id,
                    RequestedStartDate = b.RequestedStartDate,
                    RequestedEndDate = b.RequestedEndDate,
                    StartedAt = b.StartedAt,
                    CompletedAt = b.CompletedAt,
                    Status = b.Status.ToString(),
                    TotalFromSource = b.TotalFromSource,
                    NewCount = b.NewCount,
                    SkippedCount = b.SkippedCount,
                    NeedsMappingCount = b.NeedsMappingCount,
                    FailedCount = b.FailedCount,
                    DurationMs = b.DurationMs,
                    ErrorSummary = b.ErrorSummary
                })
                .ToListAsync(cancellationToken);

            return new GetImportBatchesResult { Items = items, TotalCount = total };
        }
    }
}
