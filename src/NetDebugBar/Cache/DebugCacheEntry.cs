using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace NetDebugBar.Cache;

/// <summary>
/// Wraps ICacheEntry to track Set operations when the entry is disposed.
/// </summary>
internal class DebugCacheEntry : ICacheEntry
{
    private readonly ICacheEntry _inner;
    private readonly object _key;
    private readonly NetDebugBarMemoryCache _cache;
    private readonly Stopwatch _sw = Stopwatch.StartNew();

    public DebugCacheEntry(ICacheEntry inner, object key, NetDebugBarMemoryCache cache)
    {
        _inner = inner;
        _key = key;
        _cache = cache;
    }

    public object Key => _inner.Key;

    public object? Value
    {
        get => _inner.Value;
        set => _inner.Value = value;
    }

    public DateTimeOffset? AbsoluteExpiration
    {
        get => _inner.AbsoluteExpiration;
        set => _inner.AbsoluteExpiration = value;
    }

    public TimeSpan? AbsoluteExpirationRelativeToNow
    {
        get => _inner.AbsoluteExpirationRelativeToNow;
        set => _inner.AbsoluteExpirationRelativeToNow = value;
    }

    public TimeSpan? SlidingExpiration
    {
        get => _inner.SlidingExpiration;
        set => _inner.SlidingExpiration = value;
    }

    public IList<IChangeToken> ExpirationTokens => _inner.ExpirationTokens;

    public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => _inner.PostEvictionCallbacks;

    public CacheItemPriority Priority
    {
        get => _inner.Priority;
        set => _inner.Priority = value;
    }

    public long? Size
    {
        get => _inner.Size;
        set => _inner.Size = value;
    }

    public void Dispose()
    {
        _inner.Dispose();
        _sw.Stop();
        _cache.RecordSet(_key, _inner.Value, _sw.Elapsed.TotalMilliseconds);
    }
}
