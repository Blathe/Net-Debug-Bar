using System.Text;
using NetDebugBar.Core;

namespace NetDebugBar.Rendering.Panels;

public class OverviewPanel : IDebugBarPanel
{
    private readonly NetDebugBarOptions _options;

    public OverviewPanel(NetDebugBarOptions options)
    {
        _options = options;
    }

    public string TabId => "overview";
    public string TabLabel => ".NET Debug Bar";

    public string? RenderBadge(NetDebugBarContext debug) => null;

    public string RenderTabContent(NetDebugBarContext debug)
    {
        var sb = new StringBuilder();

        // Calculate totals
        var totalQueries = debug.CurrentQueries.Count + (debug.PreviousSnapshot?.Queries.Count ?? 0);
        var totalDuration = debug.CurrentQueries.Sum(q => q.Duration) +
                           (debug.PreviousSnapshot?.Queries.Sum(q => q.Duration) ?? 0);
        var currentDuration = debug.CurrentQueries.Sum(q => q.Duration);
        var previousDuration = debug.PreviousSnapshot?.Queries.Sum(q => q.Duration) ?? 0;

        sb.Append("<div class=\"ndb-dashboard\">");

        // Total Queries card
        RenderCard(sb, "Total Queries", totalQueries.ToString(), "accent");

        // Total Duration card
        RenderCard(sb, "Total Duration", $"{totalDuration:0.##}ms");

        // Current Request card
        RenderCard(sb, "Current Request", debug.CurrentQueries.Count.ToString(),
                   subtext: $"{currentDuration:0.##}ms");

        // Previous Request card
        var prevCount = debug.PreviousSnapshot?.Queries.Count ?? 0;
        RenderCard(sb, "Previous Request", prevCount.ToString(),
                   subtext: $"{previousDuration:0.##}ms");

        // Cache Operations card (if enabled)
        if (_options.EnableCachePanel)
        {
            var cacheOps = debug.CacheOperations.Count + (debug.PreviousSnapshot?.CacheOperations.Count ?? 0);
            RenderCard(sb, "Cache Operations", cacheOps.ToString());
        }

        // Log Entries card (if enabled)
        if (_options.EnableLoggingPanel)
        {
            var logCount = debug.LogEntries.Count + (debug.PreviousSnapshot?.LogEntries.Count ?? 0);
            RenderCard(sb, "Log Entries", logCount.ToString());
        }

        // Request Duration card
        if (debug.Request != null)
        {
            RenderCard(sb, "Request Duration", $"{debug.Request.DurationMs:0.##}ms");
        }

        sb.Append("</div>");

        return sb.ToString();
    }

    private void RenderCard(StringBuilder sb, string label, string value, string cssClass = "", string? subtext = null)
    {
        var accentClass = cssClass == "accent" ? " accent" : "";
        sb.Append($"<div class=\"ndb-card{accentClass}\">");
        sb.Append($"<div class=\"ndb-card-label\">{Encode(label)}</div>");
        sb.Append($"<div class=\"ndb-card-value\">{Encode(value)}</div>");
        if (subtext != null)
        {
            sb.Append($"<div class=\"ndb-card-subtext\">{Encode(subtext)}</div>");
        }
        sb.Append("</div>");
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
}
