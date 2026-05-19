using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using IApplicationDbContext = Akyildiz.Sevkiyat.Application.Interfaces.IApplicationDbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Akyildiz.Sevkiyat.Infrastructure.BackgroundJobs;

public class YkSyncOptions
{
    public int IntervalMinutes { get; set; } = 30;
}

public class YkStatusSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<YkStatusSyncBackgroundService> _logger;
    private readonly YkSyncOptions _options;
    private readonly BackgroundServiceStatusTracker _tracker;

    // Final statuses — no point polling these again
    private static readonly HashSet<string> TerminalStatuses = ["CNL", "DLV", "ISC", "BI"];

    public YkStatusSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<YkStatusSyncBackgroundService> logger,
        IOptions<YkSyncOptions> options,
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

        _logger.LogInformation("[YK] Durum senkronizasyon servisi başlatıldı. Periyot: {IntervalMinutes} dakika.",
            interval.TotalMinutes);

        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SyncAsync(stoppingToken);
                _tracker.Record("yk-sync", BackgroundServiceRunResult.Success);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[YK] Durum senkronizasyonu sırasında beklenmeyen hata.");
                _tracker.Record("yk-sync", BackgroundServiceRunResult.Failure, ex.Message);
            }
        }
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        using var scope   = _scopeFactory.CreateScope();
        var context       = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var ykClient      = scope.ServiceProvider.GetRequiredService<IYurtiKargoClient>();

        var shipments = await context.Shipments
            .Where(s =>
                s.YkCargoKey != null &&
                s.Status == ShipmentStatus.Dispatched &&
                (s.YkOperationStatus == null || !TerminalStatuses.Contains(s.YkOperationStatus)))
            .ToListAsync(ct);

        if (shipments.Count == 0) return;

        _logger.LogInformation("[YK] Durum sorgulanacak sevkiyat sayısı: {Count}", shipments.Count);

        foreach (var shipment in shipments)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var status = await ykClient.QueryShipmentAsync(shipment.YkCargoKey!, ct);
                if (status != null)
                {
                    shipment.UpdateYkStatus(status.StatusCode, status.StatusDescription, status.Barcode);
                    _logger.LogInformation(
                        "[YK] Durum güncellendi. ShipmentId={ShipmentId} CargoKey={CargoKey} Status={Status}",
                        shipment.Id, shipment.YkCargoKey, status.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[YK] Durum sorgulanamadı. ShipmentId={ShipmentId} CargoKey={CargoKey}",
                    shipment.Id, shipment.YkCargoKey);
            }

            // Yurtiçi API: 1 dakikada tekrarlı sorgu engeli — shipment'lar arası bekleme
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        await context.SaveChangesAsync(ct);
    }
}
