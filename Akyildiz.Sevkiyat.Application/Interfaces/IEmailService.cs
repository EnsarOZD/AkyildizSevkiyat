namespace Akyildiz.Sevkiyat.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
        Task SendAsync(IEnumerable<string> to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}
