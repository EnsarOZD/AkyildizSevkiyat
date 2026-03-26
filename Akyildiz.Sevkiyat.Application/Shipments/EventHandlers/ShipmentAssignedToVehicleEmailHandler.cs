using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<ShipmentAssignedToVehicleEmailHandler> _logger;

        public ShipmentAssignedToVehicleEmailHandler(
            IApplicationDbContext context,
            IEmailService emailService,
            ILogger<ShipmentAssignedToVehicleEmailHandler> logger)
        {
            _context = context;
            _emailService = emailService;
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
                var lineCount = shipment.Lines.Count;

                var subject = $"Sevkiyat Yola Çıktı — {projectName} / {deliveryDateStr}";

                var body = $"""
                    <html><body style="font-family:Arial,sans-serif;font-size:14px;color:#333;">
                    <h2 style="color:#1d4ed8;">Sevkiyatınız Yola Çıktı</h2>
                    <table style="border-collapse:collapse;width:100%;max-width:520px;">
                      <tr><td style="padding:6px 12px;font-weight:bold;width:160px;">Proje</td><td style="padding:6px 12px;">{projectName}</td></tr>
                      <tr style="background:#f3f4f6;"><td style="padding:6px 12px;font-weight:bold;">Talep No</td><td style="padding:6px 12px;">{talepNo}</td></tr>
                      <tr><td style="padding:6px 12px;font-weight:bold;">Teslim Tarihi</td><td style="padding:6px 12px;">{deliveryDateStr}</td></tr>
                      <tr style="background:#f3f4f6;"><td style="padding:6px 12px;font-weight:bold;">Sürücü</td><td style="padding:6px 12px;">{driverName}</td></tr>
                      <tr><td style="padding:6px 12px;font-weight:bold;">Araç Plakası</td><td style="padding:6px 12px;">{plateNumber}</td></tr>
                      <tr style="background:#f3f4f6;"><td style="padding:6px 12px;font-weight:bold;">Kalem Sayısı</td><td style="padding:6px 12px;">{lineCount}</td></tr>
                    </table>
                    <p style="margin-top:20px;color:#6b7280;font-size:12px;">
                      Bu e-posta Akyildiz Sevkiyat sistemi tarafından otomatik olarak gönderilmiştir.
                    </p>
                    </body></html>
                    """;

                await _emailService.SendAsync(recipients, subject, body, cancellationToken);

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
