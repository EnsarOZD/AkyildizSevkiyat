using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Akyildiz.Sevkiyat.Infrastructure.Notifications;

public record SseNotification(string Title, string Body, string? Url, string? EventType, DateTime CreatedAt);

public class SseChannelManager
{
    private readonly ConcurrentDictionary<int, ConcurrentBag<Channel<SseNotification>>> _map = new();

    public Channel<SseNotification> Subscribe(int userId)
    {
        var channel = Channel.CreateBounded<SseNotification>(new BoundedChannelOptions(50)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });
        _map.GetOrAdd(userId, _ => new ConcurrentBag<Channel<SseNotification>>()).Add(channel);
        return channel;
    }

    public void Unsubscribe(int userId, Channel<SseNotification> channel)
    {
        channel.Writer.TryComplete();
        if (_map.TryGetValue(userId, out var bag))
        {
            // Rebuild bag without the removed channel (ConcurrentBag has no Remove)
            var remaining = bag.Where(c => !ReferenceEquals(c, channel)).ToArray();
            if (remaining.Length == 0)
                _map.TryRemove(userId, out _);
            else
            {
                var newBag = new ConcurrentBag<Channel<SseNotification>>(remaining);
                _map[userId] = newBag;
            }
        }
    }

    public void Push(int userId, SseNotification notification)
    {
        if (!_map.TryGetValue(userId, out var bag)) return;
        foreach (var channel in bag)
            channel.Writer.TryWrite(notification);
    }
}
