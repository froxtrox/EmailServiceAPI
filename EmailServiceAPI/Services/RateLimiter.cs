using System.Collections.Concurrent;

namespace EmailServiceAPI.Services;

public class RateLimiter : IDisposable
{
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new();
    private readonly ITimer _cleanupTimer;

    private const int MaxRequests = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    public RateLimiter(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _cleanupTimer = _timeProvider.CreateTimer(_ => Cleanup(), null, CleanupInterval, CleanupInterval);
    }

    public (bool Allowed, int? RetryAfterSeconds) CheckRateLimit(string ip)
    {
        var now = _timeProvider.GetUtcNow();
        var entry = _entries.GetOrAdd(ip, _ => new RateLimitEntry { WindowStart = now });

        lock (entry)
        {
            if (now - entry.WindowStart >= Window)
            {
                entry.WindowStart = now;
                entry.Count = 1;
                return (true, null);
            }

            entry.Count++;
            if (entry.Count <= MaxRequests)
                return (true, null);

            var retryAfter = (int)Math.Ceiling((entry.WindowStart + Window - now).TotalSeconds);
            return (false, retryAfter);
        }
    }

    private void Cleanup()
    {
        var now = _timeProvider.GetUtcNow();
        foreach (var kvp in _entries)
        {
            if (now - kvp.Value.WindowStart >= Window)
                _entries.TryRemove(kvp.Key, out _);
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
        GC.SuppressFinalize(this);
    }

    private class RateLimitEntry
    {
        public DateTimeOffset WindowStart { get; set; }
        public int Count { get; set; }
    }
}
