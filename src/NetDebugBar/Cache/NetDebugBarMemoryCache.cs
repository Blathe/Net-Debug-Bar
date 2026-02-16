using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using NetDebugBar.Core;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Cache;

/// <summary>
/// Decorates IMemoryCache to track all cache operations for debugging.
/// </summary>
public class NetDebugBarMemoryCache : IMemoryCache
{
    private readonly IMemoryCache _inner;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NetDebugBarMemoryCache(IMemoryCache inner, IHttpContextAccessor httpContextAccessor)
    {
        _inner = inner;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool TryGetValue(object key, out object? value)
    {
        var sw = Stopwatch.StartNew();
        var hit = _inner.TryGetValue(key, out value);
        sw.Stop();

        RecordOperation(new CacheOperationInfo
        {
            Key = key?.ToString() ?? "",
            OperationType = CacheOperationType.Get,
            IsHit = hit,
            DurationMs = sw.Elapsed.TotalMilliseconds,
            ValueTypeName = hit ? value?.GetType().Name : null,
        });

        return hit;
    }

    public ICacheEntry CreateEntry(object key)
    {
        var entry = _inner.CreateEntry(key);
        // Wrap the entry to intercept Dispose() (which is when Set actually happens)
        return new DebugCacheEntry(entry, key, this);
    }

    public void Remove(object key)
    {
        var sw = Stopwatch.StartNew();
        _inner.Remove(key);
        sw.Stop();

        RecordOperation(new CacheOperationInfo
        {
            Key = key?.ToString() ?? "",
            OperationType = CacheOperationType.Remove,
            DurationMs = sw.Elapsed.TotalMilliseconds,
        });
    }

    public void Dispose() => _inner.Dispose();

    internal void RecordSet(object key, object? value, double durationMs)
    {
        RecordOperation(new CacheOperationInfo
        {
            Key = key?.ToString() ?? "",
            OperationType = CacheOperationType.Set,
            DurationMs = durationMs,
            ValueTypeName = value?.GetType().Name,
        });
    }

    private void RecordOperation(CacheOperationInfo operation)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items["NetDebugBarContext"] is NetDebugBarContext debug)
        {
            lock (debug.CacheOperations)
            {
                debug.CacheOperations.Add(operation);
            }
        }
    }
}
