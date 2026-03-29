using Akyildiz.Sevkiyat.Application.Orders.Queries.GetImportBatches;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Orders.Queries.GetImportBatchStatus
{
    public record GetImportBatchStatusQuery(int BatchId) : IRequest<ImportBatchDto?>;

    public class GetImportBatchStatusQueryHandler : IRequestHandler<GetImportBatchStatusQuery, ImportBatchDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetImportBatchStatusQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ImportBatchDto?> Handle(GetImportBatchStatusQuery request, CancellationToken cancellationToken)
        {
            return await _context.ImportBatches
                .Where(b => b.Id == request.BatchId)
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
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
