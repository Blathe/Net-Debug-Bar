using System.Text;
using NetDebugBar.Core;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Rendering.Panels;

public class QueriesPanel : IDebugBarPanel
{
    private readonly NetDebugBarOptions _options;

    public QueriesPanel(NetDebugBarOptions options)
    {
        _options = options;
    }

    public string TabId => "queries";
    public string TabLabel => "Queries";

    public string? RenderBadge(NetDebugBarContext debug)
    {
        var total = debug.CurrentQueries.Count + (debug.PreviousSnapshot?.Queries.Count ?? 0);
        return total > 0 ? total.ToString() : null;
    }

    public string RenderTabContent(NetDebugBarContext debug)
    {
        var sb = new StringBuilder();

        // Query Legend
        sb.Append("<div style=\"margin-bottom: 12px; font-size:10px; color: #999;\">");
        sb.Append("<span class=\"ndb-query-good\">● Fast ≤ ");
        sb.Append(_options.MediumQueryThresholdMs);
        sb.Append("ms</span> ");
        sb.Append("<span style=\"margin: 0 8px;\">|</span>");
        sb.Append("<span class=\"ndb-query-medium\">● Medium ≤ ");
        sb.Append(_options.SlowQueryThresholdMs);
        sb.Append("ms</span> ");
        sb.Append("<span style=\"margin: 0 8px;\">|</span>");
        sb.Append("<span class=\"ndb-query-slow\">● Slow &gt; ");
        sb.Append(_options.SlowQueryThresholdMs);
        sb.Append("ms</span>");
        sb.Append("</div>");

        var hasPrevious = debug.PreviousSnapshot?.Queries.Any() == true;
        var hasCurrent = debug.CurrentQueries.Any();

        // Previous Queries
        if (hasPrevious)
        {
            sb.Append("<div class=\"ndb-section-header\">Previous Request (POST / Redirect)</div>");
            sb.Append("<ul class=\"ndb-queries-list\">");
            foreach (var q in debug.PreviousSnapshot!.Queries)
            {
                RenderQuery(sb, q);
            }
            sb.Append("</ul>");
        }

        // Current Queries
        if (hasCurrent)
        {
            sb.Append("<div class=\"ndb-section-header\">Current Request</div>");
            sb.Append("<ul class=\"ndb-queries-list\">");
            foreach (var q in debug.CurrentQueries)
            {
                RenderQuery(sb, q);
            }
            sb.Append("</ul>");
        }

        // No Queries Message
        if (!hasCurrent && !hasPrevious)
        {
            sb.Append("<p style=\"color: #999; margin-top: 10px;\">No queries recorded</p>");
        }

        return sb.ToString();
    }

    private void RenderQuery(StringBuilder sb, QueryInfo query)
    {
        var speedClass = GetSpeedClass(query.Duration);

        sb.Append("<li class=\"ndb-query-item\">");
        sb.Append($"<span class=\"ndb-query-duration {speedClass}\">{query.Duration:0.##}ms</span>");
        sb.Append($"<span class=\"ndb-query-sql\">{Encode(query.Sql)}</span>");

        // Render parameters if any (will be populated in Phase 4)
        if (query.Parameters.Any())
        {
            sb.Append("<details style=\"margin-left: 68px; margin-top: 4px;\">");
            sb.Append($"<summary style=\"cursor: pointer; font-size: 10px; color: #888;\">{query.Parameters.Count} parameter(s)</summary>");
            sb.Append("<table class=\"ndb-params-table\" style=\"margin-top: 4px; font-size: 10px;\">");
            foreach (var param in query.Parameters)
            {
                sb.Append("<tr>");
                sb.Append($"<td style=\"color: #6af; padding-right: 8px;\">{Encode(param.Name)}</td>");
                sb.Append($"<td style=\"color: #999; padding-right: 8px;\">{Encode(param.TypeName)}</td>");
                sb.Append($"<td style=\"color: #ddd;\">{Encode(param.Value ?? "NULL")}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            sb.Append("</details>");
        }

        sb.Append("</li>");
    }

    private string GetSpeedClass(double duration)
    {
        if (duration <= _options.MediumQueryThresholdMs)
            return "ndb-query-good";
        if (duration <= _options.SlowQueryThresholdMs)
            return "ndb-query-medium";
        return "ndb-query-slow";
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
}
