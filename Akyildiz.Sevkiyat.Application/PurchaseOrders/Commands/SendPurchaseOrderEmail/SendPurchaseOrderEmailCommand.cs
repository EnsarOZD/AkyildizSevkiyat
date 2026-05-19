using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Akyildiz.Sevkiyat.Application.PurchaseOrders.Commands.SendPurchaseOrderEmail
{
    public record SendPurchaseOrderEmailCommand(Guid Id, string? PdfBase64) : IRequest<string>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Accounting", "Manager" };
    }

    public class SendPurchaseOrderEmailCommandHandler
        : IRequestHandler<SendPurchaseOrderEmailCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly SmtpEmailOptions _smtpOptions;
        private readonly ILogger<SendPurchaseOrderEmailCommandHandler> _logger;

        public SendPurchaseOrderEmailCommandHandler(
            IApplicationDbContext context,
            IEmailService emailService,
            IOptions<SmtpEmailOptions> smtpOptions,
            ILogger<SendPurchaseOrderEmailCommandHandler> logger)
        {
            _context = context;
            _emailService = emailService;
            _smtpOptions = smtpOptions.Value;
            _logger = logger;
        }

        private static List<string> ParseEmails(string? raw) =>
            string.IsNullOrWhiteSpace(raw)
                ? []
                : raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(e => e.Trim())
                     .Where(e => e.Contains('@'))
                     .ToList();

        public async Task<string> Handle(SendPurchaseOrderEmailCommand request, CancellationToken cancellationToken)
        {
            var order = await _context.PurchaseOrders
                .Include(o => o.Supplier)
                .Include(o => o.Lines)
                    .ThenInclude(l => l.StockMaster)
                .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("Satınalma siparişi bulunamadı.");

            if (order.Status == PurchaseOrderStatus.Draft)
                throw new DomainException("Taslak durumdaki siparişler için mail gönderilemez. Lütfen önce siparişi onaylayın.");

            if (string.IsNullOrWhiteSpace(order.Supplier.Email))
                throw new DomainException("Tedarikçinin e-posta adresi tanımlı değil. Lütfen tedarikçi kaydını güncelleyin.");

            var recipientEmail = order.Supplier.Email.Trim();
            var supplierName   = System.Net.WebUtility.HtmlEncode(order.SupplierNameSnapshot);
            var orderNumber    = System.Net.WebUtility.HtmlEncode(order.OrderNumber);
            var terminStr      = order.ExpectedDeliveryDate.HasValue
                                    ? order.ExpectedDeliveryDate.Value.ToString("dd.MM.yyyy")
                                    : "—";

            var fromAddress     = !string.IsNullOrWhiteSpace(_smtpOptions.ProcurementFromAddress)
                                    ? _smtpOptions.ProcurementFromAddress
                                    : _smtpOptions.FromAddress;
            var fromDisplayName = _smtpOptions.ProcurementFromDisplayName;

            var sysSettings = await _context.SystemSettings.FindAsync(new object[] { 1 }, cancellationToken);
            var ccAddresses  = ParseEmails(sysSettings?.ProcurementEmailCc);

            var subject = $"Satın Alma Siparişi — {order.OrderNumber}";

            var linesRows = new System.Text.StringBuilder();
            foreach (var line in order.Lines.OrderBy(l => l.StockMaster.StockName))
            {
                var stockCode = System.Net.WebUtility.HtmlEncode(line.StockMaster.StockCode);
                var stockName = System.Net.WebUtility.HtmlEncode(line.StockMaster.StockName);
                var qty = line.OrderedQty.ToString("G29");
                var unit = line.Unit.ToString();
                var price = line.UnitPrice.HasValue ? line.UnitPrice.Value.ToString("N2") + " ₺" : "—";
                linesRows.Append($"""
                    <tr style="border-bottom:1px solid #f1f5f9;">
                      <td style="padding:8px 12px;font-size:12px;color:#374151;font-family:monospace;">{stockCode}</td>
                      <td style="padding:8px 12px;font-size:12px;color:#374151;">{stockName}</td>
                      <td style="padding:8px 12px;font-size:12px;color:#374151;text-align:right;">{qty}</td>
                      <td style="padding:8px 12px;font-size:12px;color:#374151;">{unit}</td>
                      <td style="padding:8px 12px;font-size:12px;color:#374151;text-align:right;">{price}</td>
                    </tr>
                    """);
            }
            var linesHtml = linesRows.ToString();

            var htmlBody = $"""
                <html>
                <body style="font-family:Arial,sans-serif;font-size:14px;color:#1f2937;background:#f3f4f6;margin:0;padding:0;">
                <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;padding:24px 0;">
                  <tr><td align="center">
                    <table width="600" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,.08);">

                      <!-- Header -->
                      <tr><td style="background:#1e3a5f;padding:24px 32px;">
                        <h1 style="margin:0;color:#ffffff;font-size:18px;font-weight:700;">SATIN ALMA SİPARİŞİ</h1>
                        <p style="margin:4px 0 0;color:#94a3b8;font-size:12px;">Akyıldız İnşaat Lojistik Tedarik ve Tesis Yönetim Hizmetleri Ltd. Şti.</p>
                      </td></tr>

                      <!-- Body -->
                      <tr><td style="padding:32px;">
                        <p style="margin:0 0 20px;">Sayın <strong>{supplierName}</strong> yetkilileri,</p>

                        <p style="margin:0 0 16px;">
                          <strong>{orderNumber}</strong> numaralı satın alma siparişimiz ekte yer almaktadır.
                        </p>

                        <ul style="margin:0 0 20px;padding-left:20px;line-height:1.9;">
                          <li>Sipariş numarasının irsaliye ve/veya faturada belirtilmesi gerekmektedir.</li>
                          <li>Siparişte belirtilen miktarların üzerinde yapılacak teslimatlar kabul edilmeyecektir.</li>
                        </ul>

                        <p style="margin:0 0 8px;font-weight:600;color:#1e3a5f;">Sipariş Kalemleri:</p>
                        <table style="width:100%;border-collapse:collapse;margin:0 0 20px;border:1px solid #e2e8f0;border-radius:6px;overflow:hidden;">
                          <thead>
                            <tr style="background:#f8fafc;border-bottom:2px solid #e2e8f0;">
                              <th style="text-align:left;padding:8px 12px;font-size:11px;color:#64748b;font-weight:600;text-transform:uppercase;">Stok Kodu</th>
                              <th style="text-align:left;padding:8px 12px;font-size:11px;color:#64748b;font-weight:600;text-transform:uppercase;">Ürün Adı</th>
                              <th style="text-align:right;padding:8px 12px;font-size:11px;color:#64748b;font-weight:600;text-transform:uppercase;">Miktar</th>
                              <th style="text-align:left;padding:8px 12px;font-size:11px;color:#64748b;font-weight:600;text-transform:uppercase;">Birim</th>
                              <th style="text-align:right;padding:8px 12px;font-size:11px;color:#64748b;font-weight:600;text-transform:uppercase;">Birim Fiyat</th>
                            </tr>
                          </thead>
                          <tbody>
                            {linesHtml}
                          </tbody>
                        </table>

                        <p style="margin:0 0 24px;">
                          <strong>Termin tarihi:</strong> {terminStr}
                        </p>

                        <p style="margin:0;color:#6b7280;font-size:13px;">Saygılarımızla,<br/><strong>Akyıldız Lojistik Satınalma Departmanı</strong></p>
                      </td></tr>

                      <!-- Footer -->
                      <tr><td style="background:#f9fafb;padding:14px 32px;border-top:1px solid #e5e7eb;">
                        <p style="margin:0;color:#9ca3af;font-size:11px;">
                          Bu e-posta Akyıldız Lojistik Satınalma Sistemi tarafından otomatik olarak gönderilmiştir.
                        </p>
                      </td></tr>

                    </table>
                  </td></tr>
                </table>
                </body>
                </html>
                """;

            // PDF eki
            List<EmailAttachment>? attachments = null;
            if (!string.IsNullOrWhiteSpace(request.PdfBase64))
            {
                try
                {
                    var pdfBytes = Convert.FromBase64String(request.PdfBase64);
                    var fileName = $"PO-{order.OrderNumber}.pdf";
                    attachments = [new EmailAttachment(fileName, pdfBytes)];
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "PDF eki dönüştürülemedi, eksiz gönderiliyor.");
                }
            }

            await _emailService.SendAsync(
                new[] { recipientEmail },
                subject,
                htmlBody,
                fromAddress,
                fromDisplayName,
                ccAddresses,
                attachments,
                cancellationToken);

            order.MarkEmailSent(recipientEmail);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Satınalma siparişi #{OrderNumber} maili {Email} adresine gönderildi.",
                order.OrderNumber, recipientEmail);

            return recipientEmail;
        }
    }

    // SmtpOptions'a erişim için application-layer DTO
    // Infrastructure'a doğrudan bağımlılık olmadığından ayrı record kullanıyoruz.
    // Program.cs'de bu options'ı application layer'a açıyoruz.
    public class SmtpEmailOptions
    {
        public string FromAddress { get; set; } = string.Empty;
        public string FromDisplayName { get; set; } = "Akyıldız Sevkiyat";
        public string ProcurementFromAddress { get; set; } = string.Empty;
        public string ProcurementFromDisplayName { get; set; } = "Akyıldız Lojistik Satınalma";

        // Dolu ise eksik/kısmi gönderim bildirimleri gerçek alıcı yerine bu adrese gider (test amaçlı)
        public string DispatchTestRecipient { get; set; } = string.Empty;
    }
}
