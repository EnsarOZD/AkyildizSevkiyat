using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Application.Interfaces;

public interface INotificationService
{
    Task SendToRolesAsync(IEnumerable<UserRole> roles, string title, string body,
        string? url = null, string? eventType = null, CancellationToken ct = default);

    Task SendToUserAsync(int userId, string title, string body,
        string? url = null, string? eventType = null, CancellationToken ct = default);
}
