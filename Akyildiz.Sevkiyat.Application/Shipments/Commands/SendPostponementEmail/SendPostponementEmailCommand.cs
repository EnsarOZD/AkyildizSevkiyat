using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.SendPurchaseOrderEmail;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.SendPostponementEmail
{
    /// <summary>
    /// Termin (teslim) tarihi ertelendiğinde projeye "sipariş saatinin gecikmesi nedeniyle
    /// siparişiniz ertelenmiştir" bildirimini gönderir. Eksik/kısmi satır karşılaştırması YOK —
    /// yalnızca yeni teslim tarihi bilgilendirmesi. Alıcı = ISS yönetici mailleri; CC = sistem
    /// CC + seçilen harici adresler.
    /// </summary>
    public record SendPostponementEmailCommand(
        int ShipmentId,
        List<string>? ExtraCc = null) : IRequest<string>;

    public class SendPostponementEmailCommandHandler : IRequestHandler<SendPostponementEmailCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly SmtpEmailOptions _smtpOptions;
        private readonly ILogger<SendPostponementEmailCommandHandler> _logger;

        public SendPostponementEmailCommandHandler(
            IApplicationDbContext context,
            IEmailService emailService,
            IOptions<SmtpEmailOptions> smtpOptions,
            ILogger<SendPostponementEmailCommandHandler> logger)
        {
            _context = context;
            _emailService = emailService;
            _smtpOptions = smtpOptions.Value;
            _logger = logger;
        }

        public async Task<string> Handle(SendPostponementEmailCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
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

            // Sistem CC + ekstra (seçilen harici) CC
            var sysSettings = await _context.SystemSettings.FindAsync(new object[] { 1 }, cancellationToken);
            var ccAddresses = ParseEmails(sysSettings?.DispatchEmailCc);
            if (request.ExtraCc != null)
                ccAddresses = ccAddresses.Union(request.ExtraCc.Where(e => e.Contains('@'))).Distinct().ToList();

            var projectName     = shipment.Project?.Name ?? "—";
            var talepNo         = shipment.TalepNo ?? shipment.IssOrder?.TalepNo ?? "—";
            var orderRef        = shipment.IssOrder?.ExternalOrderNumber ?? talepNo;
            var newDateStr      = shipment.DeliveryDate.ToString("dd.MM.yyyy");

            const string headerColor = "#d97706"; // amber
            var subject = $"Sipariş Erteleme Bildirimi — {projectName} / {newDateStr}";

            var fromAddress     = _smtpOptions.FromAddress;
            var fromDisplayName = _smtpOptions.FromDisplayName;

            var htmlBody = $"""
                <html>
                <body style="font-family:Arial,sans-serif;font-size:14px;color:#1f2937;background:#f3f4f6;margin:0;padding:0;">
                <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;padding:24px 0;">
                  <tr><td align="center">
                    <table width="640" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,.08);">

                      <!-- Header -->
                      <tr><td style="background:{headerColor};padding:24px 32px;">
                        <h1 style="margin:0;color:#ffffff;font-size:20px;font-weight:700;">Sipariş Erteleme Bildirimi</h1>
                        <p style="margin:6px 0 0;color:rgba(255,255,255,0.85);font-size:13px;">Sipariş saatinin gecikmesi nedeniyle talebiniz ertelenmiştir.</p>
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
                              <p style="margin:0;color:#6b7280;font-size:11px;font-weight:700;text-transform:uppercase;">Yeni Teslim Tarihi</p>
                              <p style="margin:4px 0 0;font-weight:700;font-size:16px;color:{headerColor};">{newDateStr}</p>
                            </td>
                          </tr>
                        </table>
                      </td></tr>

                      <!-- Divider -->
                      <tr><td style="padding:0 32px;"><hr style="border:none;border-top:1px solid #e5e7eb;"/></td></tr>

                      <!-- Note -->
                      <tr><td style="padding:16px 32px 24px;">
                        <p style="margin:0;padding:12px 16px;background:#fffbeb;border-left:4px solid {headerColor};font-size:13px;color:#374151;border-radius:0 4px 4px 0;">
                          Talebinizin sipariş saati gecikmiş olduğundan, siparişiniz yukarıda belirtilen yeni teslim tarihine ertelenmiştir.
                          Bilginize sunarız.
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

            _logger.LogInformation(
                "Sevkiyat #{ShipmentId} erteleme bildirimi {Count} adrese gönderildi (yeni tarih {Date}).",
                shipment.Id, recipients.Count, newDateStr);

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
