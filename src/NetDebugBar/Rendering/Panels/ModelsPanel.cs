using NetDebugBar.Core;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Rendering.Panels;

public class ModelsPanel : IDebugBarPanel
{
    public string TabId => "models";
    public string TabLabel => "Models";

    public string? RenderBadge(NetDebugBarContext debug)
    {
        // Count tracked entities
        var currentTracked = debug.CurrentModels?.EntityModels.Count ?? 0;
        var previousTracked = debug.PreviousSnapshot?.Models?.EntityModels.Count ?? 0;

        // Count cached entities
        var currentCached = AnalyzeCachedEntities(debug.CacheOperations).Count;
        var previousCached = debug.PreviousSnapshot != null
            ? AnalyzeCachedEntities(debug.PreviousSnapshot.CacheOperations).Count
            : 0;

        // Combine unique entity types (don't double-count if entity is both tracked and cached)
        var currentUnique = new HashSet<string>();
        currentUnique.UnionWith(debug.CurrentModels?.EntityModels.Select(e => e.TypeName) ?? Enumerable.Empty<string>());
        currentUnique.UnionWith(AnalyzeCachedEntities(debug.CacheOperations).Select(e => e.TypeName));

        var previousUnique = new HashSet<string>();
        if (debug.PreviousSnapshot?.Models != null)
        {
            previousUnique.UnionWith(debug.PreviousSnapshot.Models.EntityModels.Select(e => e.TypeName));
            previousUnique.UnionWith(AnalyzeCachedEntities(debug.PreviousSnapshot.CacheOperations).Select(e => e.TypeName));
        }

        var total = currentUnique.Count + previousUnique.Count;
        return total > 0 ? total.ToString() : null;
    }

    public string RenderTabContent(NetDebugBarContext debug)
    {
        var hasPrevious = debug.PreviousSnapshot?.Models != null;
        var hasCurrent = debug.CurrentModels != null;

        if (!hasCurrent && !hasPrevious)
            return """<p style="color: #999; margin-top: 10px;">No model data available</p>""";

        var previousContent = hasPrevious ? RenderPreviousRequest(debug.PreviousSnapshot!.Models!, debug.PreviousSnapshot.CacheOperations) : "";
        var currentContent = hasCurrent ? RenderCurrentRequest(debug.CurrentModels!, debug.CacheOperations) : "";

        return $$"""
            {{previousContent}}
            {{currentContent}}
            """;
    }

    private string RenderPreviousRequest(ModelInfo models, List<CacheOperationInfo> cacheOps)
    {
        return $$"""
            <div class="ndb-section-header">Previous Request</div>
            {{RenderModelContent(models, cacheOps)}}
            """;
    }

    private string RenderCurrentRequest(ModelInfo models, List<CacheOperationInfo> cacheOps)
    {
        return $$"""
            <div class="ndb-section-header">Current Request</div>
            {{RenderModelContent(models, cacheOps)}}
            """;
    }

    private string RenderModelContent(ModelInfo models, List<CacheOperationInfo> cacheOps)
    {
        var pageControllerInfo = RenderPageControllerInfo(models);

        // Analyze cache for entity types
        var cachedEntities = AnalyzeCachedEntities(cacheOps);

        var hasAnyEntities = models.EntityModels.Any() || cachedEntities.Any();
        if (!hasAnyEntities)
            return pageControllerInfo;

        var summary = RenderSummary(models);
        var unifiedTable = RenderUnifiedEntityTable(models.EntityModels, cachedEntities);

        return $$"""
            {{pageControllerInfo}}
            {{summary}}
            {{unifiedTable}}
            """;
    }

    private string RenderPageControllerInfo(ModelInfo models)
    {
        if (models.PageModelType != null)
        {
            return $$"""
                <div style="margin-bottom: 12px; padding: 8px; background: #1e293b; border-radius: 4px;">
                    <strong style="color: #94a3b8;">Page Model:</strong>
                    <span style="color: #e2e8f0;">{{Encode(models.PageModelType)}}</span>
                </div>
                """;
        }
        else if (models.ControllerName != null && models.ActionName != null)
        {
            return $$"""
                <div style="margin-bottom: 12px; padding: 8px; background: #1e293b; border-radius: 4px;">
                    <strong style="color: #94a3b8;">Controller:</strong>
                    <span style="color: #e2e8f0;">{{Encode(models.ControllerName)}}</span>
                    <span style="color: #64748b; margin: 0 8px;">•</span>
                    <strong style="color: #94a3b8;">Action:</strong>
                    <span style="color: #e2e8f0;">{{Encode(models.ActionName)}}</span>
                </div>
                """;
        }
        return "";
    }

    private string RenderSummary(ModelInfo models)
    {
        var totalEntities = models.EntityModels.Sum(e => e.Count);
        var totalTypes = models.EntityModels.Count;
        var totalAdded = models.EntityModels.Sum(e => e.AddedCount);
        var totalModified = models.EntityModels.Sum(e => e.ModifiedCount);
        var totalDeleted = models.EntityModels.Sum(e => e.DeletedCount);

        return $$"""
            <div class="ndb-dashboard" style="margin-bottom: 8px;">
                {{RenderCard("Total Entities", totalEntities.ToString())}}
                {{RenderCard("Entity Types", totalTypes.ToString())}}
                {{RenderCard("Added", totalAdded.ToString(), "#10b981")}}
                {{RenderCard("Modified", totalModified.ToString(), "#f59e0b")}}
                {{RenderCard("Deleted", totalDeleted.ToString(), "#ef4444")}}
            </div>
            """;
    }

    private string RenderUnifiedEntityTable(List<EntityModelInfo> trackedEntities, List<CachedEntityInfo> cachedEntities)
    {
        // Combine both tracked and cached entities into a unified view
        var allEntityTypes = new HashSet<string>();
        allEntityTypes.UnionWith(trackedEntities.Select(e => e.TypeName));
        allEntityTypes.UnionWith(cachedEntities.Select(e => e.TypeName));

        var rows = string.Join("", allEntityTypes.OrderBy(t => t).Select(typeName =>
        {
            var tracked = trackedEntities.FirstOrDefault(e => e.TypeName == typeName);
            var cached = cachedEntities.FirstOrDefault(e => e.TypeName == typeName);
            return RenderUnifiedEntityRow(typeName, tracked, cached);
        }));

        return $$"""
            <table class="ndb-table">
                <thead><tr>
                    <th>Entity Type</th>
                    <th>Total</th>
                    <th>Added</th>
                    <th>Modified</th>
                    <th>Deleted</th>
                    <th>Unchanged</th>
                    <th>Source</th>
                </tr></thead>
                <tbody>
                    {{rows}}
                </tbody>
            </table>
            """;
    }

    private string RenderUnifiedEntityRow(string typeName, EntityModelInfo? tracked, CachedEntityInfo? cached)
    {
        if (tracked != null)
        {
            // Entity is tracked by EF Core
            var addedStyle = tracked.AddedCount > 0 ? " style=\"color: #10b981; font-weight: 600;\"" : "";
            var modifiedStyle = tracked.ModifiedCount > 0 ? " style=\"color: #f59e0b; font-weight: 600;\"" : "";
            var deletedStyle = tracked.DeletedCount > 0 ? " style=\"color: #ef4444; font-weight: 600;\"" : "";

            var sourceLabel = cached != null
                ? """<span style="color: #10b981;">Database</span> + <span style="color: #3b82f6;">Cache</span>"""
                : """<span style="color: #10b981;">Database</span>""";

            return $$"""
                <tr>
                    <td><strong>{{Encode(typeName)}}</strong></td>
                    <td>{{tracked.Count}}</td>
                    <td{{addedStyle}}>{{tracked.AddedCount}}</td>
                    <td{{modifiedStyle}}>{{tracked.ModifiedCount}}</td>
                    <td{{deletedStyle}}>{{tracked.DeletedCount}}</td>
                    <td>{{tracked.UnchangedCount}}</td>
                    <td>{{sourceLabel}}</td>
                </tr>
                """;
        }
        else if (cached != null)
        {
            // Entity is only in cache (not tracked)
            return $$"""
                <tr>
                    <td><strong>{{Encode(typeName)}}</strong></td>
                    <td style="color: #3b82f6;">{{cached.TotalCount}}</td>
                    <td style="color: #64748b;">-</td>
                    <td style="color: #64748b;">-</td>
                    <td style="color: #64748b;">-</td>
                    <td style="color: #64748b;">-</td>
                    <td><span style="color: #3b82f6;">Cache</span></td>
                </tr>
                """;
        }

        return "";
    }

    private List<CachedEntityInfo> AnalyzeCachedEntities(List<CacheOperationInfo> cacheOps)
    {
        // Get cache operations that retrieved data
        var getOperations = cacheOps
            .Where(c => c.OperationType == CacheOperationType.Get && c.IsHit == true)
            .ToList();

        // Aggregate all entity types from RelatedEntityTypes
        var entityTypeCounts = new Dictionary<string, (int TotalCount, int CacheHits)>();

        foreach (var op in getOperations)
        {
            if (op.RelatedEntityTypes == null || !op.RelatedEntityTypes.Any())
                continue;

            foreach (var kvp in op.RelatedEntityTypes)
            {
                var typeName = kvp.Key;
                var count = kvp.Value;

                if (!entityTypeCounts.ContainsKey(typeName))
                    entityTypeCounts[typeName] = (0, 0);

                var current = entityTypeCounts[typeName];
                entityTypeCounts[typeName] = (current.TotalCount + count, current.CacheHits + 1);
            }
        }

        var cachedTypes = entityTypeCounts
            .Where(kvp => LooksLikeEntity(kvp.Key))
            .Select(kvp => new CachedEntityInfo
            {
                TypeName = kvp.Key,
                TotalCount = kvp.Value.TotalCount,
                CacheHits = kvp.Value.CacheHits
            })
            .OrderByDescending(c => c.TotalCount)
            .ToList();

        return cachedTypes;
    }

    private string ExtractEntityTypeName(string typeName)
    {
        // Handle generic collections like List<Game>, IEnumerable<Game>
        if (typeName.Contains('<') && typeName.Contains('>'))
        {
            var start = typeName.IndexOf('<') + 1;
            var end = typeName.LastIndexOf('>');
            if (end > start)
            {
                var innerType = typeName.Substring(start, end - start);
                // Handle nested generics - just take the first type
                if (innerType.Contains(','))
                    innerType = innerType.Split(',')[0].Trim();
                return innerType;
            }
        }

        // Handle arrays like Game[]
        if (typeName.EndsWith("[]"))
            return typeName.Substring(0, typeName.Length - 2);

        return typeName;
    }

    private bool LooksLikeEntity(string typeName)
    {
        // Filter out primitive types and common framework types
        var nonEntityTypes = new[]
        {
            "String", "Int32", "Int64", "Boolean", "DateTime", "Guid", "Decimal", "Double", "Float",
            "Byte", "Object", "Char", "Byte[]", "Dictionary", "List", "IEnumerable", "IQueryable"
        };

        return !nonEntityTypes.Contains(typeName) && !typeName.StartsWith("System.");
    }

    private string RenderCard(string label, string value, string? color = null)
    {
        var valueStyle = color != null ? $" style=\"color: {color};\"" : "";

        return $$"""
            <div class="ndb-card">
                <div class="ndb-card-label">{{Encode(label)}}</div>
                <div class="ndb-card-value"{{valueStyle}}>{{Encode(value)}}</div>
            </div>
            """;
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);

    private class CachedEntityInfo
    {
        public string TypeName { get; set; } = "";
        public int TotalCount { get; set; }
        public int CacheHits { get; set; }
    }
}
