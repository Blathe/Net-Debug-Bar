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
        // Calculate totals
        var totalQueries = debug.CurrentQueries.Count + (debug.PreviousSnapshot?.Queries.Count ?? 0);
        var totalDuration = debug.CurrentQueries.Sum(q => q.Duration) +
                           (debug.PreviousSnapshot?.Queries.Sum(q => q.Duration) ?? 0);
        var currentDuration = debug.CurrentQueries.Sum(q => q.Duration);
        var previousDuration = debug.PreviousSnapshot?.Queries.Sum(q => q.Duration) ?? 0;
        var prevCount = debug.PreviousSnapshot?.Queries.Count ?? 0;

        var cacheCard = _options.EnableCachePanel
            ? RenderCard("Cache Operations", (debug.CacheOperations.Count + (debug.PreviousSnapshot?.CacheOperations.Count ?? 0)).ToString())
            : "";

        var logsCard = _options.EnableLoggingPanel
            ? RenderCard("Log Entries", (debug.LogEntries.Count + (debug.PreviousSnapshot?.LogEntries.Count ?? 0)).ToString())
            : "";

        var requestDurationCard = debug.Request != null
            ? RenderCard("Request Duration", $"{debug.Request.DurationMs:0.##}ms")
            : "";

        return $$"""
            <div class="ndb-dashboard">
                {{RenderCard("Total Queries", totalQueries.ToString(), "accent")}}
                {{RenderCard("Total Duration", $"{totalDuration:0.##}ms")}}
                {{RenderCard("Current Request", debug.CurrentQueries.Count.ToString(), subtext: $"{currentDuration:0.##}ms")}}
                {{RenderCard("Previous Request", prevCount.ToString(), subtext: $"{previousDuration:0.##}ms")}}
                {{cacheCard}}
                {{logsCard}}
                {{requestDurationCard}}
            </div>
            """;
    }

    private string RenderCard(string label, string value, string cssClass = "", string? subtext = null)
    {
        var accentClass = cssClass == "accent" ? " accent" : "";
        var subtextHtml = subtext != null ? $"<div class=\"ndb-card-subtext\">{Encode(subtext)}</div>" : "";

        return $$"""
            <div class="ndb-card{{accentClass}}">
                <div class="ndb-card-label">{{Encode(label)}}</div>
                <div class="ndb-card-value">{{Encode(value)}}</div>
                {{subtextHtml}}
            </div>
            """;
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
}
