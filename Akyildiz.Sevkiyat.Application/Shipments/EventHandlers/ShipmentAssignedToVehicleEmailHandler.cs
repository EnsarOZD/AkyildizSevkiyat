using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.SendPurchaseOrderEmail;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Akyildiz.Sevkiyat.Application.Shipments.EventHandlers
{
    /// <summary>
    /// Sevkiyat AssignedToVehicle durumuna geçtiğinde IssOrder.YoneticiMailAdresleri listesine
    /// bildirim e-postası gönderir.
    /// </summary>
    public class ShipmentAssignedToVehicleEmailHandler : INotificationHandler<ShipmentStatusChangedEvent>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly SmtpEmailOptions _smtpOptions;
        private readonly ILogger<ShipmentAssignedToVehicleEmailHandler> _logger;

        public ShipmentAssignedToVehicleEmailHandler(
            IApplicationDbContext context,
            IEmailService emailService,
            IOptions<SmtpEmailOptions> smtpOptions,
            ILogger<ShipmentAssignedToVehicleEmailHandler> logger)
        {
            _context = context;
            _emailService = emailService;
            _smtpOptions = smtpOptions.Value;
            _logger = logger;
        }

        public async Task Handle(ShipmentStatusChangedEvent notification, CancellationToken cancellationToken)
        {
            if (notification.NewStatus != ShipmentStatus.AssignedToVehicle)
                return;

            try
            {
                var shipment = await _context.Shipments
                    .Include(s => s.IssOrder)
                    .Include(s => s.Project)
                    .Include(s => s.Lines)
                    .FirstOrDefaultAsync(s => s.Id == notification.ShipmentId, cancellationToken);

                if (shipment == null) return;

                var mailAddresses = shipment.IssOrder?.YoneticiMailAdresleri;
                if (string.IsNullOrWhiteSpace(mailAddresses)) return;

                var recipients = mailAddresses
                    .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim())
                    .Where(m => m.Contains('@'))
                    .ToList();

                if (recipients.Count == 0) return;

                var deliveryDateStr = shipment.DeliveryDate.ToString("dd.MM.yyyy");
                var projectName = shipment.Project?.Name ?? "—";
                var driverName = shipment.AssignedDriverName ?? "—";
                var plateNumber = shipment.AssignedPlateNumber ?? "—";
                var talepNo = shipment.TalepNo ?? shipment.IssOrder?.TalepNo ?? "—";

                // Eksik ürünler: DeliveredQty < OrderedQty (0 dahil — depo toplamamış)
                var missingLines = shipment.Lines
                    .Where(l => l.OrderedQty > 0 && l.DeliveredQty < l.OrderedQty)
                    .ToList();

                var hasMissing = missingLines.Any();
                var subject = hasMissing
                    ? $"⚠ Eksik Ürün Uyarısı — {projectName} / {deliveryDateStr}"
                    : $"Sevkiyat Yola Çıktı — {projectName} / {deliveryDateStr}";

                var headerColor  = hasMissing ? "#b45309" : "#1d4ed8";
                var headerBg     = hasMissing ? "#fef3c7" : "#eff6ff";
                var headerTitle  = hasMissing ? "⚠ Eksik Ürün Olan Sevkiyat Yola Çıktı" : "Sevkiyatınız Yola Çıktı";

                var missingSection = "";
                if (hasMissing)
                {
                    var missingRows = string.Join("\n", missingLines.Select((l, i) =>
                    {
                        var bg   = i % 2 == 0 ? "#fff7ed" : "#ffedd5";
                        var eksik = l.OrderedQty - l.DeliveredQty;
                        return $"""
                            <tr style="background:{bg};">
                              <td style="padding:7px 10px;border:1px solid #fed7aa;">{System.Net.WebUtility.HtmlEncode(l.StockName)}</td>
                              <td style="padding:7px 10px;border:1px solid #fed7aa;text-align:right;">{l.OrderedQty:N2}</td>
                              <td style="padding:7px 10px;border:1px solid #fed7aa;text-align:right;">{l.DeliveredQty:N2}</td>
                              <td style="padding:7px 10px;border:1px solid #fed7aa;text-align:right;color:#b45309;font-weight:bold;">-{eksik:N2}</td>
                              <td style="padding:7px 10px;border:1px solid #fed7aa;font-size:11px;color:#92400e;">{System.Net.WebUtility.HtmlEncode(l.DifferenceReason ?? "—")}</td>
                            </tr>
                            """;
                    }));

                    missingSection = $"""
                        <tr><td colspan="2" style="padding:20px 0 4px;">
                          <p style="margin:0 0 8px;font-weight:700;color:#b45309;font-size:13px;">EKSİK ÜRÜN LİSTESİ</p>
                          <table style="width:100%;border-collapse:collapse;font-size:13px;">
                            <thead>
                              <tr style="background:#d97706;color:#fff;">
                                <th style="padding:7px 10px;border:1px solid #b45309;text-align:left;">Ürün</th>
                                <th style="padding:7px 10px;border:1px solid #b45309;text-align:right;">Sipariş</th>
                                <th style="padding:7px 10px;border:1px solid #b45309;text-align:right;">Gönderilen</th>
                                <th style="padding:7px 10px;border:1px solid #b45309;text-align:right;">Eksik</th>
                                <th style="padding:7px 10px;border:1px solid #b45309;text-align:left;">Neden</th>
                              </tr>
                            </thead>
                            <tbody>{missingRows}</tbody>
                          </table>
                        </td></tr>
                        """;
                }

                var fromAddress     = _smtpOptions.FromAddress;
                var fromDisplayName = _smtpOptions.FromDisplayName;

                var sysSettings = await _context.SystemSettings.FindAsync(new object[] { 1 }, cancellationToken);

                if (sysSettings?.DispatchEmailEnabled == false)
                    return;

                var ccAddresses = (sysSettings?.DispatchEmailCc ?? string.Empty)
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => e.Contains('@'))
                    .ToList();

                var body = $"""
                    <html><body style="font-family:Arial,sans-serif;font-size:14px;color:#333;background:#f3f4f6;margin:0;padding:20px;">
                    <div style="max-width:560px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,.08);">
                      <div style="background:{headerBg};padding:20px 24px;border-bottom:3px solid {headerColor};">
                        <h2 style="margin:0;color:{headerColor};font-size:17px;">{headerTitle}</h2>
                      </div>
                      <div style="padding:20px 24px;">
                        <table style="border-collapse:collapse;width:100%;">
                          <tr><td style="padding:6px 0;font-weight:bold;width:140px;">Proje</td><td style="padding:6px 0;">{System.Net.WebUtility.HtmlEncode(projectName)}</td></tr>
                          <tr style="background:#f9fafb;"><td style="padding:6px 4px;font-weight:bold;">Talep No</td><td style="padding:6px 4px;">{System.Net.WebUtility.HtmlEncode(talepNo)}</td></tr>
                          <tr><td style="padding:6px 0;font-weight:bold;">Teslim Tarihi</td><td style="padding:6px 0;">{deliveryDateStr}</td></tr>
                          <tr style="background:#f9fafb;"><td style="padding:6px 4px;font-weight:bold;">Araç / Sürücü</td><td style="padding:6px 4px;">{System.Net.WebUtility.HtmlEncode(plateNumber)} / {System.Net.WebUtility.HtmlEncode(driverName)}</td></tr>
                          {missingSection}
                        </table>
                      </div>
                      <div style="background:#f9fafb;padding:12px 24px;border-top:1px solid #e5e7eb;">
                        <p style="margin:0;color:#6b7280;font-size:11px;">Bu e-posta Akyıldız Sevkiyat Sistemi tarafından otomatik gönderilmiştir.</p>
                      </div>
                    </div>
                    </body></html>
                    """;

                await _emailService.SendAsync(recipients, subject, body, fromAddress, fromDisplayName, ccAddresses, cancellationToken);

                _logger.LogInformation(
                    "Sevkiyat #{ShipmentId} yükleme bildirimi {Count} adrese gönderildi.",
                    shipment.Id, recipients.Count);
            }
            catch (Exception ex)
            {
                // Mail hatası sevkiyat akışını bloke etmemeli
                _logger.LogError(ex, "Sevkiyat #{ShipmentId} için e-posta gönderilemedi.", notification.ShipmentId);
            }
        }
    }
}
