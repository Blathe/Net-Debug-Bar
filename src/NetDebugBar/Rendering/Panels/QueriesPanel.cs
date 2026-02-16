using NetDebugBar.Core;
using NetDebugBar.Core.Models;
using NetDebugBar.Utils;

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

        // Detect N+1 if enabled
        if (_options.EnableNPlusOneDetection && debug.CurrentQueries.Any())
        {
            debug.NPlusOneGroups = NPlusOneDetector.DetectNPlusOne(debug.CurrentQueries, _options.NPlusOneThreshold);

            if (debug.NPlusOneGroups.Any())
            {
                return $"{total} ⚠️";
            }
        }

        return total > 0 ? total.ToString() : null;
    }

    public string RenderTabContent(NetDebugBarContext debug)
    {
        var legend = RenderLegend();
        var hasPrevious = debug.PreviousSnapshot?.Queries.Any() == true;
        var hasCurrent = debug.CurrentQueries.Any();

        var nPlusOneWarnings = _options.EnableNPlusOneDetection && debug.NPlusOneGroups.Any()
            ? RenderNPlusOneWarnings(debug.NPlusOneGroups)
            : "";

        var previousQueries = hasPrevious ? RenderPreviousQueries(debug.PreviousSnapshot!.Queries) : "";
        var currentQueries = hasCurrent ? RenderCurrentQueries(debug.CurrentQueries) : "";
        var noQueries = !hasCurrent && !hasPrevious ? """<p style="color: #999; margin-top: 10px;">No queries recorded</p>""" : "";

        return $$"""
            {{legend}}
            {{nPlusOneWarnings}}
            {{previousQueries}}
            {{currentQueries}}
            {{noQueries}}
            """;
    }

    private string RenderLegend()
    {
        return $$"""
            <div style="margin-bottom: 12px; font-size:10px; color: #999;">
                <span class="ndb-query-good">● Fast ≤ {{_options.MediumQueryThresholdMs}}ms</span>
                <span style="margin: 0 8px;">|</span>
                <span class="ndb-query-medium">● Medium ≤ {{_options.SlowQueryThresholdMs}}ms</span>
                <span style="margin: 0 8px;">|</span>
                <span class="ndb-query-slow">● Slow &gt; {{_options.SlowQueryThresholdMs}}ms</span>
            </div>
            """;
    }

    private string RenderNPlusOneWarnings(List<NPlusOneGroup> groups)
    {
        var warnings = string.Join("", groups.Select(RenderNPlusOneWarning));

        return $$"""
            <div style="background: rgba(255, 200, 0, 0.1); border-left: 3px solid #fc0; padding: 8px; margin-bottom: 12px; border-radius: 2px;">
                <div style="font-weight: bold; color: #fc0; margin-bottom: 6px; font-size: 12px;">⚠️ N+1 Query Problems Detected</div>
                {{warnings}}
            </div>
            """;
    }

    private string RenderNPlusOneWarning(NPlusOneGroup group)
    {
        var queriesHtml = string.Join("", group.Queries.Select(RenderQuery));

        return $$"""
            <details style="margin-bottom: 8px;" open>
                <summary style="cursor: pointer; font-size: 11px; color: #fc0; user-select: none; padding: 4px 0;">
                    <strong>{{group.Count}} similar queries</strong> ({{group.TotalDuration:0.##}}ms total) - Possible N+1 problem
                </summary>
                <div style="margin-left: 12px; margin-top: 6px;">
                    <div style="font-size: 10px; color: #999; margin-bottom: 6px;">Pattern: {{Encode(group.NormalizedSql)}}</div>
                    <ul class="ndb-queries-list" style="margin: 0; padding-left: 15px;">
                        {{queriesHtml}}
                    </ul>
                </div>
            </details>
            """;
    }

    private string RenderPreviousQueries(IEnumerable<QueryInfo> queries)
    {
        var queryItems = string.Join("", queries.Select(RenderQuery));

        return $$"""
            <div class="ndb-section-header">Previous Request (POST / Redirect)</div>
            <ul class="ndb-queries-list">
                {{queryItems}}
            </ul>
            """;
    }

    private string RenderCurrentQueries(IEnumerable<QueryInfo> queries)
    {
        var queryItems = string.Join("", queries.Select(RenderQuery));

        return $$"""
            <div class="ndb-section-header">Current Request</div>
            <ul class="ndb-queries-list">
                {{queryItems}}
            </ul>
            """;
    }

    private string RenderQuery(QueryInfo query)
    {
        var speedClass = GetSpeedClass(query.Duration);
        var parameters = query.Parameters.Any() ? RenderParameters(query.Parameters) : "";

        return $$"""
            <li class="ndb-query-item">
                <span class="ndb-query-duration {{speedClass}}">{{query.Duration:0.##}}ms</span>
                <span class="ndb-query-sql">{{Encode(query.Sql)}}</span>
                {{parameters}}
            </li>
            """;
    }

    private string RenderParameters(IEnumerable<QueryParameterInfo> parameters)
    {
        var count = parameters.Count();
        var rows = string.Join("", parameters.Select(param => $$"""
            <tr>
                <td style="color: #6af; padding-right: 8px;">{{Encode(param.Name)}}</td>
                <td style="color: #999; padding-right: 8px;">{{Encode(param.TypeName)}}</td>
                <td style="color: #ddd;">{{Encode(param.Value ?? "NULL")}}</td>
            </tr>
            """));

        return $$"""
            <details style="margin-left: 68px; margin-top: 4px;">
                <summary style="cursor: pointer; font-size: 10px; color: #888;">{{count}} parameter(s)</summary>
                <table class="ndb-params-table" style="margin-top: 4px; font-size: 10px;">
                    {{rows}}
                </table>
            </details>
            """;
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
