using Akyildiz.Sevkiyat.Application.Common.Interfaces;

namespace Akyildiz.Sevkiyat.Application.Orders.Commands.ImportIssOrders
{
    public class ImportIssOrdersCommandHandler : IRequestHandler<ImportIssOrdersCommand, ImportIssOrdersResult>
    {
        private readonly IIssOrderImportOrchestrator _orchestrator;

        public ImportIssOrdersCommandHandler(IIssOrderImportOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        public async Task<ImportIssOrdersResult> Handle(ImportIssOrdersCommand request, CancellationToken cancellationToken)
        {
            var r = await _orchestrator.RunAsync(request.StartDate, request.EndDate, cancellationToken);
            return new ImportIssOrdersResult
            {
                TotalFromIss = r.TotalFromSource,
                NewCount = r.Added,
                SkippedCount = r.Skipped,
                NeedsMappingCount = r.NeedsMapping,
                FailedCount = r.Errors,
                BatchId = r.BatchId,
                Errors = r.ErrorMessages
            };
        }
    }
}
