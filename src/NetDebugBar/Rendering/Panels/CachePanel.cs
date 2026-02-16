using System.Text;
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
        var sb = new StringBuilder();

        var hasPrevious = debug.PreviousSnapshot?.CacheOperations.Any() == true;
        var hasCurrent = debug.CacheOperations.Any();

        if (!hasCurrent && !hasPrevious)
        {
            sb.Append("<p style=\"color: #999; margin-top: 10px;\">No cache operations recorded</p>");
            return sb.ToString();
        }

        // Summary cards
        var allOps = debug.CacheOperations.Concat(debug.PreviousSnapshot?.CacheOperations ?? Enumerable.Empty<CacheOperationInfo>()).ToList();
        var gets = allOps.Count(o => o.OperationType == CacheOperationType.Get);
        var hits = allOps.Count(o => o.OperationType == CacheOperationType.Get && o.IsHit == true);
        var misses = allOps.Count(o => o.OperationType == CacheOperationType.Get && o.IsHit == false);
        var sets = allOps.Count(o => o.OperationType == CacheOperationType.Set);
        var removes = allOps.Count(o => o.OperationType == CacheOperationType.Remove);
        var hitRate = gets > 0 ? (hits * 100.0 / gets) : 0;

        sb.Append("<div class=\"ndb-dashboard\" style=\"margin-bottom: 8px;\">");
        RenderCard(sb, "Total Operations", allOps.Count.ToString());
        RenderCard(sb, "Gets", gets.ToString());
        RenderCard(sb, "Hits", hits.ToString(), "#10b981");
        RenderCard(sb, "Misses", misses.ToString(), "#ef4444");
        RenderCard(sb, "Hit Rate", $"{hitRate:0.#}%", hitRate > 75 ? "#10b981" : hitRate > 50 ? "#f59e0b" : "#ef4444");
        RenderCard(sb, "Sets", sets.ToString());
        RenderCard(sb, "Removes", removes.ToString());
        sb.Append("</div>");

        // Previous Operations
        if (hasPrevious)
        {
            sb.Append("<div class=\"ndb-section-header\">Previous Request</div>");
            sb.Append("<table class=\"ndb-table ndb-cache-table\">");
            sb.Append("<thead><tr>");
            sb.Append("<th>Operation</th>");
            sb.Append("<th>Key</th>");
            sb.Append("<th>Result</th>");
            sb.Append("<th>Duration</th>");
            sb.Append("<th>Type</th>");
            sb.Append("</tr></thead><tbody>");
            foreach (var op in debug.PreviousSnapshot!.CacheOperations)
            {
                RenderOperation(sb, op);
            }
            sb.Append("</tbody></table>");
        }

        // Current Operations
        if (hasCurrent)
        {
            sb.Append("<div class=\"ndb-section-header\">Current Request</div>");
            sb.Append("<table class=\"ndb-table ndb-cache-table\">");
            sb.Append("<thead><tr>");
            sb.Append("<th>Operation</th>");
            sb.Append("<th>Key</th>");
            sb.Append("<th>Result</th>");
            sb.Append("<th>Duration</th>");
            sb.Append("<th>Type</th>");
            sb.Append("</tr></thead><tbody>");
            foreach (var op in debug.CacheOperations)
            {
                RenderOperation(sb, op);
            }
            sb.Append("</tbody></table>");
        }

        return sb.ToString();
    }

    private void RenderOperation(StringBuilder sb, CacheOperationInfo op)
    {
        sb.Append("<tr>");

        // Operation Type
        var opColor = op.OperationType switch
        {
            CacheOperationType.Get => "#3b82f6",
            CacheOperationType.Set => "#10b981",
            CacheOperationType.Remove => "#ef4444",
            _ => "#999"
        };
        sb.Append($"<td style=\"color: {opColor};\">{op.OperationType}</td>");

        // Key
        sb.Append($"<td>{Encode(op.Key)}</td>");

        // Result (Hit/Miss for Get operations)
        if (op.OperationType == CacheOperationType.Get)
        {
            var resultColor = op.IsHit == true ? "#10b981" : "#ef4444";
            var resultText = op.IsHit == true ? "HIT" : "MISS";
            sb.Append($"<td style=\"color: {resultColor};\">{resultText}</td>");
        }
        else
        {
            sb.Append("<td>-</td>");
        }

        // Duration
        sb.Append($"<td>{op.DurationMs:0.##}ms</td>");

        // Value Type
        sb.Append($"<td>{Encode(op.ValueTypeName ?? "-")}</td>");

        sb.Append("</tr>");
    }

    private void RenderCard(StringBuilder sb, string label, string value, string? color = null)
    {
        sb.Append("<div class=\"ndb-card\">");
        sb.Append($"<div class=\"ndb-card-label\">{Encode(label)}</div>");
        if (color != null)
        {
            sb.Append($"<div class=\"ndb-card-value\" style=\"color: {color};\">{Encode(value)}</div>");
        }
        else
        {
            sb.Append($"<div class=\"ndb-card-value\">{Encode(value)}</div>");
        }
        sb.Append("</div>");
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
}
