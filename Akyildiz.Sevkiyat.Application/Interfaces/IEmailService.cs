namespace Akyildiz.Sevkiyat.Application.Interfaces
{
    public record EmailAttachment(string FileName, byte[] Data, string MimeType = "application/pdf");

    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
        Task SendAsync(IEnumerable<string> to, string subject, string htmlBody, CancellationToken cancellationToken = default);

        // Belirli bir gönderen adresiyle gönderim (ör. procurement@, dispatch@)
        Task SendAsync(IEnumerable<string> to, string subject, string htmlBody, string fromAddress, string fromDisplayName, CancellationToken cancellationToken = default);

        // CC desteğiyle gönderim
        Task SendAsync(IEnumerable<string> to, string subject, string htmlBody, string fromAddress, string fromDisplayName, IEnumerable<string>? cc, CancellationToken cancellationToken = default);

        // Ek (attachment) desteğiyle gönderim
        Task SendAsync(IEnumerable<string> to, string subject, string htmlBody, string fromAddress, string fromDisplayName, IEnumerable<string>? cc, IEnumerable<EmailAttachment>? attachments, CancellationToken cancellationToken = default);
    }
}
