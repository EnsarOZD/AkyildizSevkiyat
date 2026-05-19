using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Akyildiz.Sevkiyat.Infrastructure.BackgroundJobs;

public class NetsisIrsaliyeSyncOptions
{
    public int IntervalMinutes { get; set; } = 30;
    public int LookbackDays    { get; set; } = 60;
    public int BatchSize       { get; set; } = 50;
}

/// <summary>
/// NetsisTransferredAt dolu ama IrsaliyeNo boş sevkiyatlarda irsaliye numarasını
/// Netsis'ten periyodik olarak çeker. Steady-state'de yapacak iş kalmadığında
/// herhangi bir Netsis isteği atmaz.
/// </summary>
public class NetsisIrsaliyeSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NetsisIrsaliyeSyncBackgroundService> _logger;
    private readonly NetsisIrsaliyeSyncOptions _options;
    private readonly BackgroundServiceStatusTracker _tracker;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public NetsisIrsaliyeSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<NetsisIrsaliyeSyncBackgroundService> logger,
        IOptions<NetsisIrsaliyeSyncOptions> options,
        BackgroundServiceStatusTracker tracker)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _options      = options.Value;
        _tracker      = tracker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_options.IntervalMinutes > 0 ? _options.IntervalMinutes : 30);

        _logger.LogInformation(
            "Netsis irsaliye senkronizasyon servisi başlatıldı. Periyot: {Minutes} dakika.", interval.TotalMinutes);

        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!await _semaphore.WaitAsync(0, stoppingToken))
            {
                _logger.LogWarning("Netsis irsaliye sync önceki çalışma henüz bitmedi, bu tick atlanıyor.");
                continue;
            }

            try
            {
                await RunSyncAsync(stoppingToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope  = _scopeFactory.CreateScope();
            var context      = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var netsisClient = scope.ServiceProvider.GetRequiredService<INetsisClient>();

            var lookback = DateTime.UtcNow.AddDays(-(_options.LookbackDays > 0 ? _options.LookbackDays : 60));

            var candidates = await context.Shipments
                .Include(s => s.IssOrder)
                .Where(s => s.NetsisTransferredAt.HasValue
                         && s.IrsaliyeNo == null
                         && s.DeliveryDate >= lookback
                         && s.IssOrder != null
                         && s.IssOrder.NetsisOrderNumber != null)
                .OrderBy(s => s.Id)
                .Take(_options.BatchSize > 0 ? _options.BatchSize : 50)
                .ToListAsync(cancellationToken);

            if (!candidates.Any())
                return;

            _logger.LogInformation(
                "Netsis irsaliye sync: {Count} sevkiyat için irsaliye numarası çekiliyor.", candidates.Count);

            int synced = 0;
            foreach (var shipment in candidates)
            {
                try
                {
                    var irsaliyeler = await netsisClient.GetIrsaliyelerAsync(
                        new NetsisIrsaliyeQuery { SiparisNo = shipment.IssOrder!.NetsisOrderNumber },
                        cancellationToken);

                    if (irsaliyeler?.Any() == true)
                    {
                        var ilk = irsaliyeler.First();
                        shipment.SetIrsaliyeInfo(ilk.IrsaliyeNo, ilk.IrsaliyeTarihi);
                        synced++;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Netsis irsaliye sync: Sevkiyat #{Id} irsaliye çekimi başarısız — atlanıyor.", shipment.Id);
                }
            }

            if (synced > 0)
                await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Netsis irsaliye sync tamamlandı: {Synced}/{Total} güncellendi.", synced, candidates.Count);
            _tracker.Record("netsis-irsaliye", BackgroundServiceRunResult.Success);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Netsis irsaliye sync iptal edildi (uygulama kapanıyor).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Netsis irsaliye sync sırasında beklenmeyen hata oluştu.");
            _tracker.Record("netsis-irsaliye", BackgroundServiceRunResult.Failure, ex.Message);
        }
    }
}
