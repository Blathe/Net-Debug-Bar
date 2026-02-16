using Microsoft.Extensions.Logging;
using NetDebugBar.Core;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Rendering.Panels;

public class LoggingPanel : IDebugBarPanel
{
    public string TabId => "logs";
    public string TabLabel => "Logs";

    public string? RenderBadge(NetDebugBarContext debug)
    {
        var total = debug.LogEntries.Count + (debug.PreviousSnapshot?.LogEntries.Count ?? 0);
        return total > 0 ? total.ToString() : null;
    }

    public string RenderTabContent(NetDebugBarContext debug)
    {
        var hasPrevious = debug.PreviousSnapshot?.LogEntries.Any() == true;
        var hasCurrent = debug.LogEntries.Any();

        if (!hasCurrent && !hasPrevious)
            return """<p style="color: #999; margin-top: 10px;">No log entries recorded</p>""";

        var summary = RenderSummary(debug);
        var filters = RenderFilters(debug);
        var previousLogs = hasPrevious ? RenderPreviousLogs(debug.PreviousSnapshot!.LogEntries) : "";
        var currentLogs = hasCurrent ? RenderCurrentLogs(debug.LogEntries) : "";

        return $$"""
            {{summary}}
            {{filters}}
            {{previousLogs}}
            {{currentLogs}}
            """;
    }

    private string RenderSummary(NetDebugBarContext debug)
    {
        var allLogs = debug.LogEntries.Concat(debug.PreviousSnapshot?.LogEntries ?? Enumerable.Empty<LogEntry>()).ToList();
        var byLevel = allLogs.GroupBy(l => l.Level).OrderBy(g => g.Key);
        var levelCards = string.Join("", byLevel.Select(group =>
        {
            var color = GetLogLevelColor(group.Key);
            return $$"""
                <div class="ndb-card">
                    <div class="ndb-card-label">{{group.Key}}</div>
                    <div class="ndb-card-value" style="color: {{color}};">{{group.Count()}}</div>
                </div>
                """;
        }));

        return $$"""
            <div class="ndb-dashboard" style="margin-bottom: 16px;">
                <div class="ndb-card accent">
                    <div class="ndb-card-label">Total Logs</div>
                    <div class="ndb-card-value">{{allLogs.Count}}</div>
                </div>
                {{levelCards}}
            </div>
            """;
    }

    private string RenderFilters(NetDebugBarContext debug)
    {
        var allLogs = debug.LogEntries.Concat(debug.PreviousSnapshot?.LogEntries ?? Enumerable.Empty<LogEntry>()).ToList();
        var buttons = string.Join("", Enum.GetValues<LogLevel>()
            .Where(level => level != LogLevel.None)
            .Select(level =>
            {
                var count = allLogs.Count(l => l.Level == level);
                if (count == 0) return "";

                var color = GetLogLevelColor(level);
                return $$"""
                    <button class="ndb-log-filter active" data-level="{{level}}" style="background: {{color}}; border: none; color: #000; padding: 4px 8px; margin: 2px; border-radius: 3px; cursor: pointer; font-size: 10px; font-weight: bold;">
                        {{level}} ({{count}})
                    </button>
                    """;
            }));

        return $"""<div style="margin-bottom: 12px;">{buttons}</div>""";
    }

    private string RenderPreviousLogs(IEnumerable<LogEntry> logs)
    {
        var entries = string.Join("", logs.Select(RenderLogEntry));
        return $$"""
            <div class="ndb-section-header">Previous Request</div>
            {{entries}}
            """;
    }

    private string RenderCurrentLogs(IEnumerable<LogEntry> logs)
    {
        var entries = string.Join("", logs.Select(RenderLogEntry));
        return $$"""
            <div class="ndb-section-header">Current Request</div>
            {{entries}}
            """;
    }

    private string RenderLogEntry(LogEntry log)
    {
        var color = GetLogLevelColor(log.Level);
        var levelBadge = GetLogLevelBadge(log.Level);
        var exception = !string.IsNullOrEmpty(log.Exception) ? $$"""
            <details style="margin-top: 4px;">
                <summary style="color: #ef4444; font-size: 10px; cursor: pointer;">Exception Details</summary>
                <pre style="color: #ef4444; font-size: 10px; margin-top: 4px; white-space: pre-wrap; font-family: 'Courier New', monospace;">{{Encode(log.Exception)}}</pre>
            </details>
            """ : "";

        return $$"""
            <div class="ndb-log-entry" data-level="{{log.Level}}" style="margin: 8px 0; padding: 8px; background: rgba(255,255,255,0.03); border-left: 3px solid {{color}}; border-radius: 2px;">
                <div style="display: flex; align-items: center; margin-bottom: 4px;">
                    <span style="color: #666; font-size: 10px; margin-right: 8px;">{{log.Timestamp:HH:mm:ss.fff}}</span>
                    <span style="background: {{color}}; color: #000; padding: 2px 6px; border-radius: 3px; font-size: 10px; font-weight: bold; margin-right: 8px;">{{levelBadge}}</span>
                    <span style="color: #999; font-size: 10px; font-family: 'Courier New', monospace;">{{Encode(log.Category)}}</span>
                </div>
                <div style="color: #ddd; font-size: 11px; line-height: 1.4;">{{Encode(log.Message)}}</div>
                {{exception}}
            </div>
            """;
    }

    private string GetLogLevelColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "#6b7280",       // gray
            LogLevel.Debug => "#9ca3af",       // light gray
            LogLevel.Information => "#3b82f6", // blue
            LogLevel.Warning => "#f59e0b",     // amber
            LogLevel.Error => "#ef4444",       // red
            LogLevel.Critical => "#dc2626",    // dark red
            _ => "#999"
        };
    }

    private string GetLogLevelBadge(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "???"
        };
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
}
