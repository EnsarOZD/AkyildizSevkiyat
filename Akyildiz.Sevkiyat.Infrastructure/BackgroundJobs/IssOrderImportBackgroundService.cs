using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Akyildiz.Sevkiyat.Infrastructure.BackgroundJobs;

public class IssImportOptions
{
    public int IntervalMinutes { get; set; } = 15;
    public int LookbackDays { get; set; } = 1;
}

public class IssOrderImportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IssOrderImportBackgroundService> _logger;
    private readonly IssImportOptions _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public IssOrderImportBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<IssOrderImportBackgroundService> logger,
        IOptions<IssImportOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _options.IntervalMinutes > 0 ? _options.IntervalMinutes : 15;
        var lookbackDays = _options.LookbackDays > 0 ? _options.LookbackDays : 1;

        _logger.LogInformation(
            "ISS-IP import background service başlatıldı. Periyot: {IntervalMinutes} dakika, Geri bakış: {LookbackDays} gün.",
            intervalMinutes, lookbackDays);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!await _semaphore.WaitAsync(0, stoppingToken))
            {
                _logger.LogWarning("ISS-IP import önceki çalışma henüz tamamlanmadı, bu tick atlanıyor.");
                continue;
            }

            try
            {
                await RunImportAsync(stoppingToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    private async Task RunImportAsync(CancellationToken cancellationToken)
    {
        var lookbackDays = _options.LookbackDays > 0 ? _options.LookbackDays : 1;
        var end = DateTime.UtcNow;
        var start = end.AddDays(-lookbackDays);

        _logger.LogInformation(
            "ISS-IP otomatik import başlıyor. Aralık: {Start:yyyy-MM-dd} - {End:yyyy-MM-dd}",
            start, end);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IIssOrderImportOrchestrator>();
            var result = await orchestrator.RunAsync(start, end, cancellationToken);

            _logger.LogInformation(
                "ISS-IP otomatik import tamamlandı. Toplam: {Total}, Eklenen: {Added}, Atlanan: {Skipped}, Eşleştirme Bekleyen: {NeedsMapping}, Hatalı: {Errors}, BatchId: {BatchId}",
                result.TotalFromSource, result.Added, result.Skipped, result.NeedsMapping, result.Errors, result.BatchId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ISS-IP import iptal edildi (uygulama kapanıyor).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ISS-IP otomatik import sırasında beklenmeyen hata oluştu.");
        }
    }
}
