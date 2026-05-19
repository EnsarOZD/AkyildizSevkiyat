using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WebPush;

namespace Akyildiz.Sevkiyat.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SseChannelManager _sse;
    private readonly VapidOptions _vapid;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IServiceScopeFactory scopeFactory,
        SseChannelManager sse,
        IOptions<VapidOptions> vapid,
        ILogger<NotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _sse = sse;
        _vapid = vapid.Value;
        _logger = logger;
    }

    public async Task SendToRolesAsync(IEnumerable<UserRole> roles, string title, string body,
        string? url = null, string? eventType = null, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var roleInts = roles.Select(r => (int)r).ToList();
        var userIds = await context.Users
            .Where(u => u.IsActive && roleInts.Contains((int)u.Role))
            .Select(u => u.Id)
            .ToListAsync(ct);

        foreach (var id in userIds)
            await SendToUserInternalAsync(context, id, title, body, url, eventType, ct);
    }

    public async Task SendToUserAsync(int userId, string title, string body,
        string? url = null, string? eventType = null, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        await SendToUserInternalAsync(context, userId, title, body, url, eventType, ct);
    }

    private async Task SendToUserInternalAsync(IApplicationDbContext context, int userId,
        string title, string body, string? url, string? eventType, CancellationToken ct)
    {
        // 1. Save to DB
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Url = url,
            EventType = eventType ?? string.Empty,
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync(ct);

        // 2. Push to SSE channel (in-app, real-time)
        _sse.Push(userId, new SseNotification(title, body, url, eventType, notification.CreatedAt));

        // 3. Send Web Push (for when browser tab is closed)
        await SendWebPushAsync(context, userId, title, body, url, ct);
    }

    private async Task SendWebPushAsync(IApplicationDbContext context, int userId,
        string title, string body, string? url, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_vapid.PublicKey)) return;

        var subscriptions = await context.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);

        if (subscriptions.Count == 0) return;

        var payload = JsonSerializer.Serialize(new { title, body, url });
        var client = new WebPushClient();
        var vapidDetails = new VapidDetails(_vapid.Subject, _vapid.PublicKey, _vapid.PrivateKey);

        var toRemove = new List<int>();
        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256DH, sub.Auth);
                await client.SendNotificationAsync(pushSub, payload, vapidDetails, ct);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone
                                               || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Subscription expired — clean up
                toRemove.Add(sub.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Web Push gönderilemedi. UserId={UserId} Endpoint={Ep}", userId, sub.Endpoint);
            }
        }

        if (toRemove.Count > 0)
        {
            await context.PushSubscriptions
                .Where(s => toRemove.Contains(s.Id))
                .ExecuteDeleteAsync(ct);
        }
    }
}
