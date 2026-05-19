namespace Akyildiz.Sevkiyat.Infrastructure.BackgroundJobs;

public enum BackgroundServiceRunResult { Success, Failure }

public record BackgroundServiceRun(DateTime RunAt, BackgroundServiceRunResult Result, string? ErrorMessage);

public class BackgroundServiceStatusTracker
{
    private readonly Dictionary<string, BackgroundServiceRun> _lastRuns = new();
    private readonly Lock _lock = new();

    public void Record(string service, BackgroundServiceRunResult result, string? error = null)
    {
        lock (_lock)
        {
            _lastRuns[service] = new BackgroundServiceRun(DateTime.UtcNow, result, error);
        }
    }

    public IReadOnlyDictionary<string, BackgroundServiceRun> GetAll()
    {
        lock (_lock)
        {
            return new Dictionary<string, BackgroundServiceRun>(_lastRuns);
        }
    }
}
