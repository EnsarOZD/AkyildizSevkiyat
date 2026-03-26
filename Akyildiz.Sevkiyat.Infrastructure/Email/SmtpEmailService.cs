using Akyildiz.Sevkiyat.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

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

        public async Task SendAsync(IEnumerable<string> to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var recipients = to.ToList();
            if (recipients.Count == 0) return;

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = new NetworkCredential(_options.UserName, _options.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromDisplayName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            foreach (var recipient in recipients)
                message.To.Add(recipient);

            try
            {
                await client.SendMailAsync(message, cancellationToken);
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
