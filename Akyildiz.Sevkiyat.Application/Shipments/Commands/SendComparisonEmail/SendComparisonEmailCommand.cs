using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.SendPurchaseOrderEmail;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.SendComparisonEmail
{
    /// <summary>
    /// Sevkiyat karşılaştırma raporundan "Eksik Ürün" veya "Kısmi Gönderim"
    /// bildirimi e-postasını SMTP üzerinden gönderir ve gönderim tarihini kaydeder.
    /// </summary>
    public record SendComparisonEmailCommand(
        int ShipmentId,
        List<string>? ExtraCc = null,
        string? CancellationReason = null) : IRequest<string>;

    public class SendComparisonEmailCommandHandler : IRequestHandler<SendComparisonEmailCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly SmtpEmailOptions _smtpOptions;
        private readonly ILogger<SendComparisonEmailCommandHandler> _logger;

        public SendComparisonEmailCommandHandler(
            IApplicationDbContext context,
            IEmailService emailService,
            IOptions<SmtpEmailOptions> smtpOptions,
            ILogger<SendComparisonEmailCommandHandler> logger)
        {
            _context = context;
            _emailService = emailService;
            _smtpOptions = smtpOptions.Value;
            _logger = logger;
        }

        public async Task<string> Handle(SendComparisonEmailCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                    .ThenInclude(o => o!.Lines)
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Sevkiyat bulunamadı.");

            var mailAddresses = shipment.IssOrder?.YoneticiMailAdresleri;
            if (string.IsNullOrWhiteSpace(mailAddresses))
                throw new DomainException("Bu sevkiyata ait ISS siparişinde yönetici e-posta adresi tanımlı değil.");

            var recipients = mailAddresses
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim())
                .Where(m => m.Contains('@'))
                .ToList();

            if (recipients.Count == 0)
                throw new DomainException("Geçerli e-posta adresi bulunamadı.");

            // Test modu: gerçek alıcı yerine override adresine gönder
            if (!string.IsNullOrWhiteSpace(_smtpOptions.DispatchTestRecipient))
            {
                _logger.LogWarning(
                    "DispatchTestRecipient aktif — gerçek alıcı ({Real}) yerine {Test} adresine gönderiliyor.",
                    string.Join(", ", recipients), _smtpOptions.DispatchTestRecipient);
                recipients = [_smtpOptions.DispatchTestRecipient.Trim()];
            }

            // Load CC addresses from SystemSettings + merge extra CC from request
            var sysSettings = await _context.SystemSettings.FindAsync(new object[] { 1 }, cancellationToken);
            var ccAddresses = ParseEmails(sysSettings?.DispatchEmailCc);
            if (request.ExtraCc != null)
                ccAddresses = ccAddresses.Union(request.ExtraCc.Where(e => e.Contains('@'))).Distinct().ToList();

            // Build line comparison
            var issLines      = shipment.IssOrder?.Lines?.ToList() ?? [];
            var shipmentLines = shipment.Lines.ToList();
            var linkedMap     = shipmentLines
                .Where(l => l.IssOrderLineId.HasValue)
                .ToDictionary(l => l.IssOrderLineId!.Value);

            // Eksik / kısmi satırlar (ISS sipariş bazlı)
            var problemLines = new List<(string StockCode, string StockName, decimal IssQty, decimal ActualQty, string? Reason, bool IsMissing)>();
            foreach (var issLine in issLines)
            {
                if (linkedMap.TryGetValue(issLine.Id, out var sl))
                {
                    if (sl.DeliveredQty < issLine.OrderedQty)
                        problemLines.Add((issLine.StockCode, issLine.StockName,
                            issLine.OrderedQty, sl.DeliveredQty, sl.DifferenceReason,
                            IsMissing: sl.DeliveredQty == 0));
                }
                else
                {
                    problemLines.Add((issLine.StockCode, issLine.StockName,
                        issLine.OrderedQty, 0, null, IsMissing: true));
                }
            }

            // Ekstra satırlar — ISS siparişinde olmayan, sevkiyata manuel eklenen kalemler
            var extraLines = shipmentLines
                .Where(l => !l.IssOrderLineId.HasValue)
                .Select(l => (StockCode: l.StockCode, StockName: l.StockName, ActualQty: l.DeliveredQty))
                .ToList();

            if (problemLines.Count == 0 && extraLines.Count == 0)
                throw new DomainException("Bu sevkiyatta bildirim gerektiren satır bulunamadı.");

            bool hasMissing = problemLines.Any(l => l.IsMissing);
            var deliveryDateStr = shipment.DeliveryDate.ToString("dd.MM.yyyy");
            var projectName     = shipment.Project?.Name ?? "—";
            var talepNo         = shipment.TalepNo ?? shipment.IssOrder?.TalepNo ?? "—";
            var orderRef        = shipment.IssOrder?.ExternalOrderNumber ?? talepNo;

            bool isCancellation = !string.IsNullOrWhiteSpace(request.CancellationReason);

            var subject     = isCancellation
                ? $"Sipariş İptal Bildirimi — {projectName} / {deliveryDateStr}"
                : $"Eksik / Farklı Ürün Bildirimi — {projectName} / {deliveryDateStr}";
            var headerColor = (hasMissing || isCancellation) ? "#dc2626" : "#d97706";
            var headerTitle = isCancellation
                ? "Sipariş İptal / Gönderilemedi Bildirimi"
                : "Eksik / Farklı Ürün Bildirimi";
            var headerSubtitle = isCancellation
                ? "Talebiniz stokta olmadığı için gönderilememiştir."
                : hasMissing
                    ? "Aşağıdaki ürünler eksik veya farklı olarak gönderilmiştir."
                    : "Aşağıdaki ürünler talep edilen miktarın altında veya farklı olarak gönderilmiştir.";

            // Eksik / kısmi satır tablosu
            var problemRowsHtml = problemLines.Count > 0
                ? string.Join("\n", problemLines.Select((l, i) =>
                {
                    var bg        = i % 2 == 0 ? "#ffffff" : "#f9fafb";
                    var shortfall = l.IssQty - l.ActualQty;
                    var actualColor = l.IsMissing ? "#dc2626" : "#d97706";
                    var code   = System.Net.WebUtility.HtmlEncode(l.StockCode);
                    var name   = System.Net.WebUtility.HtmlEncode(l.StockName);
                    var reason = l.Reason != null ? System.Net.WebUtility.HtmlEncode(l.Reason) : "—";
                    return $"""
                        <tr style="background:{bg};">
                          <td style="padding:8px 12px;border:1px solid #e5e7eb;font-family:monospace;font-size:12px;">{code}</td>
                          <td style="padding:8px 12px;border:1px solid #e5e7eb;">{name}</td>
                          <td style="padding:8px 12px;border:1px solid #e5e7eb;text-align:right;">{l.IssQty:N2}</td>
                          <td style="padding:8px 12px;border:1px solid #e5e7eb;text-align:right;color:{actualColor};font-weight:600;">{l.ActualQty:N2}</td>
                          <td style="padding:8px 12px;border:1px solid #e5e7eb;text-align:right;color:#dc2626;font-weight:700;">-{shortfall:N2}</td>
                          <td style="padding:8px 12px;border:1px solid #e5e7eb;font-size:12px;color:#6b7280;">{reason}</td>
                        </tr>
                        """;
                }))
                : string.Empty;

            var problemTableHtml = problemLines.Count > 0 ? $"""
                <table style="width:100%;border-collapse:collapse;font-size:13px;">
                  <thead>
                    <tr style="background:{headerColor};color:#ffffff;">
                      <th style="padding:8px 12px;border:1px solid rgba(0,0,0,.15);text-align:left;font-weight:600;">Stok Kodu</th>
                      <th style="padding:8px 12px;border:1px solid rgba(0,0,0,.15);text-align:left;font-weight:600;">Ürün Adı</th>
                      <th style="padding:8px 12px;border:1px solid rgba(0,0,0,.15);text-align:right;font-weight:600;">Sipariş</th>
                      <th style="padding:8px 12px;border:1px solid rgba(0,0,0,.15);text-align:right;font-weight:600;">Gönderilen</th>
                      <th style="padding:8px 12px;border:1px solid rgba(0,0,0,.15);text-align:right;font-weight:600;">Eksik</th>
                      <th style="padding:8px 12px;border:1px solid rgba(0,0,0,.15);text-align:left;font-weight:600;">Açıklama</th>
                    </tr>
                  </thead>
                  <tbody>{problemRowsHtml}</tbody>
                </table>
                """ : string.Empty;

            // Ekstra satır tablosu
            var extraRowsHtml = string.Join("\n", extraLines.Select((l, i) =>
            {
                var bg   = i % 2 == 0 ? "#ffffff" : "#f0fdf4";
                var code = System.Net.WebUtility.HtmlEncode(l.StockCode);
                var name = System.Net.WebUtility.HtmlEncode(l.StockName);
                return $"""
                    <tr style="background:{bg};">
                      <td style="padding:8px 12px;border:1px solid #e5e7eb;font-family:monospace;font-size:12px;">{code}</td>
                      <td style="padding:8px 12px;border:1px solid #e5e7eb;">{name}</td>
                      <td style="padding:8px 12px;border:1px solid #e5e7eb;text-align:right;color:#059669;font-weight:700;">{l.ActualQty:N2}</td>
                    </tr>
                    """;
            }));

            var extraTableHtml = extraLines.Count > 0 ? $"""
                <p style="margin:20px 0 8px;font-size:13px;font-weight:700;color:#0369a1;">Yerine Gönderilen / Ekstra Ürünler</p>
                <table style="width:100%;border-collapse:collapse;font-size:13px;">
                  <thead>
                    <tr style="background:#0369a1;color:#ffffff;">
                      <th style="padding:8px 12px;border:1px solid rgba(0,0,0,.15);text-align:left;font-weight:600;">Stok Kodu</th>
                      <th style="padding:8px 12px;border:1px solid rgba(0,0,0,.15);text-align:left;font-weight:600;">Ürün Adı</th>
                      <th style="padding:8px 12px;border:1px solid rgba(0,0,0,.15);text-align:right;font-weight:600;">Gönderilen Miktar</th>
                    </tr>
                  </thead>
                  <tbody>{extraRowsHtml}</tbody>
                </table>
                """ : string.Empty;

            var rowsHtml = problemTableHtml + extraTableHtml;

            var closingNote = isCancellation
                ? $"Bu sipariş depo tarafından karşılanamadığı için iptal edilmiştir (Sebep: {System.Net.WebUtility.HtmlEncode(request.CancellationReason)}). " +
                  "İhtiyaç duyulması halinde yeni bir sipariş oluşturmanız gerekmektedir."
                : hasMissing
                    ? "Eksik ürünler bir daha gönderilmeyecektir. İhtiyaç duyulması halinde yeni bir sipariş oluşturmanız gerekmektedir."
                    : "Eksik miktar için talep oluşturmanız gerekebilir.";

            var fromAddress     = !string.IsNullOrWhiteSpace(_smtpOptions.FromAddress)
                                    ? _smtpOptions.FromAddress
                                    : _smtpOptions.FromAddress;
            var fromDisplayName = _smtpOptions.FromDisplayName;

            var htmlBody = $"""
                <html>
                <body style="font-family:Arial,sans-serif;font-size:14px;color:#1f2937;background:#f3f4f6;margin:0;padding:0;">
                <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;padding:24px 0;">
                  <tr><td align="center">
                    <table width="640" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,.08);">

                      <!-- Header -->
                      <tr><td style="background:{headerColor};padding:24px 32px;">
                        <h1 style="margin:0;color:#ffffff;font-size:20px;font-weight:700;">{headerTitle}</h1>
                        <p style="margin:6px 0 0;color:rgba(255,255,255,0.85);font-size:13px;">{headerSubtitle}</p>
                      </td></tr>

                      <!-- Order Info -->
                      <tr><td style="padding:24px 32px 16px;">
                        <table style="width:100%;border-collapse:collapse;">
                          <tr>
                            <td style="width:50%;vertical-align:top;">
                              <table style="border-collapse:collapse;">
                                <tr>
                                  <td style="padding:4px 10px 4px 0;color:#6b7280;font-size:11px;font-weight:700;text-transform:uppercase;white-space:nowrap;">Proje</td>
                                  <td style="padding:4px 0;font-weight:600;">{System.Net.WebUtility.HtmlEncode(projectName)}</td>
                                </tr>
                                <tr>
                                  <td style="padding:4px 10px 4px 0;color:#6b7280;font-size:11px;font-weight:700;text-transform:uppercase;white-space:nowrap;">Talep No</td>
                                  <td style="padding:4px 0;">{System.Net.WebUtility.HtmlEncode(talepNo)}</td>
                                </tr>
                                <tr>
                                  <td style="padding:4px 10px 4px 0;color:#6b7280;font-size:11px;font-weight:700;text-transform:uppercase;white-space:nowrap;">Sipariş No</td>
                                  <td style="padding:4px 0;">{System.Net.WebUtility.HtmlEncode(orderRef)}</td>
                                </tr>
                              </table>
                            </td>
                            <td style="width:50%;vertical-align:top;text-align:right;">
                              <p style="margin:0;color:#6b7280;font-size:11px;font-weight:700;text-transform:uppercase;">Teslim Tarihi</p>
                              <p style="margin:4px 0 0;font-weight:700;font-size:16px;">{deliveryDateStr}</p>
                            </td>
                          </tr>
                        </table>
                      </td></tr>

                      <!-- Divider -->
                      <tr><td style="padding:0 32px;"><hr style="border:none;border-top:1px solid #e5e7eb;"/></td></tr>

                      <!-- Lines -->
                      <tr><td style="padding:16px 32px 24px;">
                        {rowsHtml}
                        <p style="margin:16px 0 0;padding:12px 16px;background:#fef2f2;border-left:4px solid {headerColor};font-size:13px;color:#374151;border-radius:0 4px 4px 0;">
                          {closingNote}
                        </p>
                      </td></tr>

                      <!-- Footer -->
                      <tr><td style="background:#f9fafb;padding:16px 32px;border-top:1px solid #e5e7eb;">
                        <p style="margin:0;color:#6b7280;font-size:11px;">
                          Bu e-posta <strong>Akyıldız Lojistik Sevkiyat Sistemi</strong> tarafından otomatik olarak gönderilmiştir.<br/>
                          Sorularınız için bu e-postayı yanıtlayabilirsiniz.
                        </p>
                      </td></tr>

                    </table>
                  </td></tr>
                </table>
                </body>
                </html>
                """;

            await _emailService.SendAsync(
                recipients,
                subject,
                htmlBody,
                fromAddress,
                fromDisplayName,
                ccAddresses,
                cancellationToken);

            shipment.MarkMissingItemsMailSent();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Sevkiyat #{ShipmentId} ({Type}) bildirimi {Count} adrese gönderildi.",
                shipment.Id, hasMissing ? "eksik" : "kısmi", recipients.Count);

            return string.Join(", ", recipients);
        }

        private static List<string> ParseEmails(string? raw) =>
            string.IsNullOrWhiteSpace(raw)
                ? []
                : raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(e => e.Trim())
                     .Where(e => e.Contains('@'))
                     .ToList();
    }
}
