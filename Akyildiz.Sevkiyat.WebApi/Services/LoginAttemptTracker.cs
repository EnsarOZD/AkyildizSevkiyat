using System.Collections.Concurrent;

namespace Akyildiz.Sevkiyat.WebApi.Services
{
    public interface ILoginAttemptTracker
    {
        bool IsBlocked(string ip);
        void RecordAttempt(string ip);
        void Reset(string ip);
        IReadOnlyList<(string Ip, int Attempts, DateTime BlockedUntil)> GetBlockedList();
    }

    public class LoginAttemptTracker : ILoginAttemptTracker
    {
        private readonly int _maxAttempts;
        private readonly TimeSpan _window;

        private record AttemptRecord(int Count, DateTime WindowStart);
        private readonly ConcurrentDictionary<string, AttemptRecord> _store = new();

        public LoginAttemptTracker(int maxAttempts = 5, int windowMinutes = 5)
        {
            _maxAttempts = maxAttempts;
            _window = TimeSpan.FromMinutes(windowMinutes);
        }

        public bool IsBlocked(string ip)
        {
            if (!_store.TryGetValue(ip, out var record)) return false;
            if (DateTime.UtcNow - record.WindowStart > _window)
            {
                _store.TryRemove(ip, out _);
                return false;
            }
            return record.Count >= _maxAttempts;
        }

        public void RecordAttempt(string ip)
        {
            _store.AddOrUpdate(ip,
                _ => new AttemptRecord(1, DateTime.UtcNow),
                (_, existing) =>
                {
                    if (DateTime.UtcNow - existing.WindowStart > _window)
                        return new AttemptRecord(1, DateTime.UtcNow);
                    return existing with { Count = existing.Count + 1 };
                });
        }

        public void Reset(string ip)
        {
            _store.TryRemove(ip, out _);
        }

        public IReadOnlyList<(string Ip, int Attempts, DateTime BlockedUntil)> GetBlockedList()
        {
            var now = DateTime.UtcNow;
            return _store
                .Where(kv => kv.Value.Count >= _maxAttempts && now - kv.Value.WindowStart <= _window)
                .Select(kv => (kv.Key, kv.Value.Count, kv.Value.WindowStart + _window))
                .ToList();
        }
    }
}
