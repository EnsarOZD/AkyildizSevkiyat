namespace Akyildiz.Sevkiyat.Application.Common.Interfaces;

public record IssOrderImportResult(
    int TotalFromSource,
    int Added,
    int Skipped,
    int NeedsMapping,
    int Errors,
    int BatchId,
    List<string> ErrorMessages);

public interface IIssOrderImportOrchestrator
{
    Task<IssOrderImportResult> RunAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
}
