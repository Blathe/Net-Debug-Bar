using NetDebugBar.Core;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Rendering.Panels;

public class CachePanel : IDebugBarPanel
{
    public string TabId => "cache";
    public string TabLabel => "Cache";

    public string? RenderBadge(NetDebugBarContext debug)
    {
        var total = debug.CacheOperations.Count + (debug.PreviousSnapshot?.CacheOperations.Count ?? 0);
        return total > 0 ? total.ToString() : null;
    }

    public string RenderTabContent(NetDebugBarContext debug)
    {
        var hasPrevious = debug.PreviousSnapshot?.CacheOperations.Any() == true;
        var hasCurrent = debug.CacheOperations.Any();

        if (!hasCurrent && !hasPrevious)
            return """<p style="color: #999; margin-top: 10px;">No cache operations recorded</p>""";

        var summary = RenderSummary(debug);
        var previousOps = hasPrevious ? RenderPreviousOperations(debug.PreviousSnapshot!.CacheOperations) : "";
        var currentOps = hasCurrent ? RenderCurrentOperations(debug.CacheOperations) : "";

        return $$"""
            {{summary}}
            {{previousOps}}
            {{currentOps}}
            """;
    }

    private string RenderSummary(NetDebugBarContext debug)
    {
        var allOps = debug.CacheOperations.Concat(debug.PreviousSnapshot?.CacheOperations ?? Enumerable.Empty<CacheOperationInfo>()).ToList();
        var gets = allOps.Count(o => o.OperationType == CacheOperationType.Get);
        var hits = allOps.Count(o => o.OperationType == CacheOperationType.Get && o.IsHit == true);
        var misses = allOps.Count(o => o.OperationType == CacheOperationType.Get && o.IsHit == false);
        var sets = allOps.Count(o => o.OperationType == CacheOperationType.Set);
        var removes = allOps.Count(o => o.OperationType == CacheOperationType.Remove);
        var hitRate = gets > 0 ? (hits * 100.0 / gets) : 0;
        var hitRateColor = hitRate > 75 ? "#10b981" : hitRate > 50 ? "#f59e0b" : "#ef4444";

        return $$"""
            <div class="ndb-dashboard" style="margin-bottom: 8px;">
                {{RenderCard("Total Operations", allOps.Count.ToString())}}
                {{RenderCard("Gets", gets.ToString())}}
                {{RenderCard("Hits", hits.ToString(), "#10b981")}}
                {{RenderCard("Misses", misses.ToString(), "#ef4444")}}
                {{RenderCard("Hit Rate", $"{hitRate:0.#}%", hitRateColor)}}
                {{RenderCard("Sets", sets.ToString())}}
                {{RenderCard("Removes", removes.ToString())}}
            </div>
            """;
    }

    private string RenderPreviousOperations(IEnumerable<CacheOperationInfo> operations)
    {
        var rows = string.Join("", operations.Select(RenderOperation));

        return $$"""
            <div class="ndb-section-header">Previous Request</div>
            <table class="ndb-table ndb-cache-table">
                <thead><tr>
                    <th>Operation</th>
                    <th>Key</th>
                    <th>Result</th>
                    <th>Duration</th>
                    <th>Type</th>
                </tr></thead>
                <tbody>
                    {{rows}}
                </tbody>
            </table>
            """;
    }

    private string RenderCurrentOperations(IEnumerable<CacheOperationInfo> operations)
    {
        var rows = string.Join("", operations.Select(RenderOperation));

        return $$"""
            <div class="ndb-section-header">Current Request</div>
            <table class="ndb-table ndb-cache-table">
                <thead><tr>
                    <th>Operation</th>
                    <th>Key</th>
                    <th>Result</th>
                    <th>Duration</th>
                    <th>Type</th>
                </tr></thead>
                <tbody>
                    {{rows}}
                </tbody>
            </table>
            """;
    }

    private string RenderOperation(CacheOperationInfo op)
    {
        var opColor = op.OperationType switch
        {
            CacheOperationType.Get => "#3b82f6",
            CacheOperationType.Set => "#10b981",
            CacheOperationType.Remove => "#ef4444",
            _ => "#999"
        };

        var result = op.OperationType == CacheOperationType.Get
            ? $"<td style=\"color: {(op.IsHit == true ? "#10b981" : "#ef4444")};\">{(op.IsHit == true ? "HIT" : "MISS")}</td>"
            : "<td>-</td>";

        return $$"""
            <tr>
                <td style="color: {{opColor}};">{{op.OperationType}}</td>
                <td>{{Encode(op.Key)}}</td>
                {{result}}
                <td>{{op.DurationMs:0.##}}ms</td>
                <td>{{Encode(op.ValueTypeName ?? "-")}}</td>
            </tr>
            """;
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
}
