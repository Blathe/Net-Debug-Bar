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
            ValueTypeName = hit && value != null ? GetFriendlyTypeName(value.GetType()) : null,
            CollectionCount = hit && value != null ? GetCollectionCount(value) : null,
            RelatedEntityTypes = hit && value != null ? AnalyzeRelatedEntities(value) : new(),
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
            ValueTypeName = value != null ? GetFriendlyTypeName(value.GetType()) : null,
            CollectionCount = value != null ? GetCollectionCount(value) : null,
            RelatedEntityTypes = value != null ? AnalyzeRelatedEntities(value) : new(),
        });
    }

    private static string GetFriendlyTypeName(Type type)
    {
        // Handle arrays
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            return elementType != null ? $"{elementType.Name}[]" : type.Name;
        }

        // Handle generic types like List<T>, IEnumerable<T>
        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                var baseName = type.Name.Split('`')[0]; // Get "List" from "List`1"
                var argNames = string.Join(", ", genericArgs.Select(t => t.Name));
                return $"{baseName}<{argNames}>";
            }
        }

        return type.Name;
    }

    private static int? GetCollectionCount(object value)
    {
        // Try to get count from ICollection
        if (value is System.Collections.ICollection collection)
            return collection.Count;

        // Try to count IEnumerable (this will enumerate, so only do for non-ICollection)
        if (value is System.Collections.IEnumerable enumerable)
        {
            var count = 0;
            foreach (var _ in enumerable)
            {
                count++;
                if (count > 10000) return null; // Safety limit to avoid huge enumerations
            }
            return count;
        }

        return null;
    }

    private static Dictionary<string, int> AnalyzeRelatedEntities(object value)
    {
        var entityCounts = new Dictionary<string, int>();
        var visitedObjects = new HashSet<object>(new ObjectReferenceEqualityComparer());

        try
        {
            // If it's a collection, analyze each item
            if (value is System.Collections.IEnumerable enumerable and not string)
            {
                var itemCount = 0;
                foreach (var item in enumerable)
                {
                    if (item == null) continue;

                    itemCount++;
                    if (itemCount > 100) break; // Analyze first 100 items max for performance

                    AnalyzeObject(item, entityCounts, visitedObjects, depth: 0);
                }
            }
            else
            {
                // Single object
                AnalyzeObject(value, entityCounts, visitedObjects, depth: 0);
            }
        }
        catch
        {
            // Silently fail on any reflection errors
        }

        return entityCounts;
    }

    private static void AnalyzeObject(object obj, Dictionary<string, int> entityCounts, HashSet<object> visitedObjects, int depth)
    {
        if (obj == null || depth > 2) return; // Limit depth to avoid circular references

        // Check if we've already visited this exact object instance
        if (!visitedObjects.Add(obj))
            return; // Already counted this object

        var type = obj.GetType();

        // Skip primitive types and common framework types
        if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) || type == typeof(Guid) || type == typeof(decimal) ||
            type.Namespace?.StartsWith("System") == true)
            return;

        // This looks like an entity - count it
        var typeName = type.Name;
        if (!entityCounts.ContainsKey(typeName))
            entityCounts[typeName] = 0;
        entityCounts[typeName]++;

        // Analyze properties (navigation properties)
        var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var prop in properties)
        {
            try
            {
                // Skip indexed properties
                if (prop.GetIndexParameters().Length > 0) continue;

                var propValue = prop.GetValue(obj);
                if (propValue == null) continue;

                var propType = prop.PropertyType;

                // Check if it's a collection navigation property
                if (propValue is System.Collections.IEnumerable collection and not string)
                {
                    var collectionItemCount = 0;
                    foreach (var collectionItem in collection)
                    {
                        if (collectionItem == null) continue;
                        collectionItemCount++;
                        if (collectionItemCount > 50) break; // Limit items analyzed

                        AnalyzeObject(collectionItem, entityCounts, visitedObjects, depth + 1);
                    }
                }
                // Check if it's a reference navigation property
                else if (!propType.IsPrimitive && propType != typeof(string) &&
                         propType != typeof(DateTime) && propType != typeof(DateTimeOffset) &&
                         propType != typeof(Guid) && propType != typeof(decimal) &&
                         propType.Namespace?.StartsWith("System") != true)
                {
                    AnalyzeObject(propValue, entityCounts, visitedObjects, depth + 1);
                }
            }
            catch
            {
                // Skip properties that throw exceptions
                continue;
            }
        }
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

    // Comparer that uses reference equality to track unique object instances
    private class ObjectReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
