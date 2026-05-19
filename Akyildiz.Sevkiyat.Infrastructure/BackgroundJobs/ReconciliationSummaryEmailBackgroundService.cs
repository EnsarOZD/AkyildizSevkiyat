using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Akyildiz.Sevkiyat.Infrastructure.BackgroundJobs;

public class ReconciliationSummaryEmailOptions
{
    /// <summary>UTC saati (0-23) — bu saatte günlük özet gönderilir. Default: 06:00 UTC.</summary>
    public int SendHourUtc { get; set; } = 6;

    /// <summary>Alıcı e-posta adresleri (virgülle ayrılmış string veya dizi).</summary>
    public List<string> Recipients { get; set; } = new();
}

public class ReconciliationSummaryEmailBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReconciliationSummaryEmailBackgroundService> _logger;
    private readonly ReconciliationSummaryEmailOptions _options;

    public ReconciliationSummaryEmailBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReconciliationSummaryEmailBackgroundService> logger,
        IOptions<ReconciliationSummaryEmailOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Recipients.Count == 0)
        {
            _logger.LogInformation("Mutabakat özet e-postası: alıcı yapılandırılmamış. Servis pasif.");
            return;
        }

        _logger.LogInformation(
            "Mutabakat özet e-posta servisi başlatıldı. Saat (UTC): {Hour:D2}:00, Alıcılar: {Recipients}",
            _options.SendHourUtc, string.Join(", ", _options.Recipients));

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextSend();
            _logger.LogDebug("Mutabakat özet e-postası {Delay} sonra gönderilecek.", delay);

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            await TrySendSummaryAsync(stoppingToken);
        }
    }

    private TimeSpan TimeUntilNextSend()
    {
        var now = DateTime.UtcNow;
        var next = new DateTime(now.Year, now.Month, now.Day, _options.SendHourUtc, 0, 0, DateTimeKind.Utc);
        if (next <= now)
            next = next.AddDays(1);
        return next - now;
    }

    private async Task TrySendSummaryAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var openIssues = await context.ReconciliationIssues
                .Where(i => i.Status == ReconciliationStatus.Open)
                .GroupBy(i => i.CheckType)
                .Select(g => new { CheckType = g.Key.ToString(), Count = g.Count() })
                .ToListAsync(ct);

            var totalOpen = openIssues.Sum(x => x.Count);

            if (totalOpen == 0)
            {
                _logger.LogInformation("Mutabakat özet: açık sorun yok. E-posta gönderilmedi.");
                return;
            }

            var rows = string.Join("", openIssues.Select(x =>
                $"<tr><td style='padding:6px 12px;border-bottom:1px solid #e5e7eb'>{x.CheckType}</td>" +
                $"<td style='padding:6px 12px;border-bottom:1px solid #e5e7eb;font-weight:bold;color:#dc2626'>{x.Count}</td></tr>"));

            var html = $"""
                <div style="font-family:sans-serif;max-width:600px;margin:0 auto">
                  <h2 style="color:#1e3a5f;border-bottom:2px solid #e5e7eb;padding-bottom:8px">
                    Mutabakat Kontrol Özeti — {DateTime.UtcNow:dd MMMM yyyy}
                  </h2>
                  <p style="color:#374151">Sistemde <strong>{totalOpen} adet açık mutabakat sorunu</strong> bulunmaktadır.</p>
                  <table style="width:100%;border-collapse:collapse;margin:16px 0">
                    <thead>
                      <tr style="background:#f3f4f6">
                        <th style="padding:8px 12px;text-align:left;font-size:12px;color:#6b7280;text-transform:uppercase">Kontrol Tipi</th>
                        <th style="padding:8px 12px;text-align:left;font-size:12px;color:#6b7280;text-transform:uppercase">Açık Sorun</th>
                      </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                  </table>
                  <p style="color:#6b7280;font-size:13px">
                    Sorunları incelemek için sisteme giriş yapın → Mutabakat Kontrolleri
                  </p>
                </div>
                """;

            await emailService.SendAsync(
                _options.Recipients,
                $"[Sevkiyat] {totalOpen} Açık Mutabakat Sorunu — {DateTime.UtcNow:dd.MM.yyyy}",
                html,
                ct);

            _logger.LogInformation(
                "Mutabakat özet e-postası gönderildi. Açık sorun: {Count}, Alıcılar: {Recipients}",
                totalOpen, string.Join(", ", _options.Recipients));
        }
        catch (OperationCanceledException)
        {
            // app shutting down
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mutabakat özet e-postası gönderilemedi.");
        }
    }
}
