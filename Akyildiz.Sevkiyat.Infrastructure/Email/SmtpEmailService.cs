using Akyildiz.Sevkiyat.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Akyildiz.Sevkiyat.Infrastructure.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
            => SendAsync(new[] { to }, subject, htmlBody, cancellationToken);

        public Task SendAsync(IEnumerable<string> to, string subject, string htmlBody, CancellationToken cancellationToken = default)
            => SendAsync(to, subject, htmlBody, _options.FromAddress, _options.FromDisplayName, null, cancellationToken);

        public Task SendAsync(IEnumerable<string> to, string subject, string htmlBody, string fromAddress, string fromDisplayName, CancellationToken cancellationToken = default)
            => SendAsync(to, subject, htmlBody, fromAddress, fromDisplayName, null, null, cancellationToken);

        public Task SendAsync(IEnumerable<string> to, string subject, string htmlBody, string fromAddress, string fromDisplayName, IEnumerable<string>? cc, CancellationToken cancellationToken = default)
            => SendAsync(to, subject, htmlBody, fromAddress, fromDisplayName, cc, null, cancellationToken);

        public async Task SendAsync(IEnumerable<string> to, string subject, string htmlBody, string fromAddress, string fromDisplayName, IEnumerable<string>? cc, IEnumerable<EmailAttachment>? attachments, CancellationToken cancellationToken = default)
        {
            var recipients = to.ToList();
            if (recipients.Count == 0) return;

            bool isProcurement = !string.IsNullOrWhiteSpace(_options.ProcurementFromAddress)
                && string.Equals(fromAddress.Trim(), _options.ProcurementFromAddress.Trim(), StringComparison.OrdinalIgnoreCase);

            var userName = isProcurement && !string.IsNullOrWhiteSpace(_options.ProcurementUserName)
                ? _options.ProcurementUserName
                : _options.UserName;
            var password = isProcurement && !string.IsNullOrWhiteSpace(_options.ProcurementPassword)
                ? _options.ProcurementPassword
                : _options.Password;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromDisplayName, fromAddress));
            foreach (var recipient in recipients)
                message.To.Add(MailboxAddress.Parse(recipient));
            if (cc != null)
                foreach (var ccAddr in cc)
                    message.Cc.Add(MailboxAddress.Parse(ccAddr));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            if (attachments != null)
                foreach (var att in attachments)
                    bodyBuilder.Attachments.Add(att.FileName, att.Data, MimeKit.ContentType.Parse(att.MimeType));
            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using var client = new SmtpClient();

                if (_options.SkipCertificateValidation)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                var sslOptions = _options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
                await client.ConnectAsync(_options.Host, _options.Port, sslOptions, cancellationToken);
                await client.AuthenticateAsync(userName, password, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation("Email sent to {Recipients} | Subject: {Subject}", string.Join(", ", recipients), subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipients} | Subject: {Subject}", string.Join(", ", recipients), subject);
                throw;
            }
        }
    }
}
