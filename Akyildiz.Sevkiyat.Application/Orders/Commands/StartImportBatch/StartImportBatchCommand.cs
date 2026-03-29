using Akyildiz.Sevkiyat.Application.Common.Interfaces;

namespace Akyildiz.Sevkiyat.Application.Orders.Commands.StartImportBatch
{
    public record StartImportBatchCommand(DateTime StartDate, DateTime EndDate)
        : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting" };
    }

    public class StartImportBatchCommandHandler : IRequestHandler<StartImportBatchCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public StartImportBatchCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(StartImportBatchCommand request, CancellationToken cancellationToken)
        {
            var batch = new Domain.Entities.ImportBatch
            {
                RequestedStartDate = request.StartDate,
                RequestedEndDate = request.EndDate,
                StartedAt = DateTime.UtcNow,
                Status = ImportBatchStatus.Running
            };
            _context.ImportBatches.Add(batch);
            await _context.SaveChangesAsync(cancellationToken);
            return batch.Id;
        }
    }
}
