using System.Text;
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
        var sb = new StringBuilder();

        var hasPrevious = debug.PreviousSnapshot?.LogEntries.Any() == true;
        var hasCurrent = debug.LogEntries.Any();

        if (!hasCurrent && !hasPrevious)
        {
            sb.Append("<p style=\"color: #999; margin-top: 10px;\">No log entries recorded</p>");
            return sb.ToString();
        }

        // Summary cards
        var allLogs = debug.LogEntries.Concat(debug.PreviousSnapshot?.LogEntries ?? Enumerable.Empty<LogEntry>()).ToList();
        var byLevel = allLogs.GroupBy(l => l.Level).OrderBy(g => g.Key).ToList();

        sb.Append("<div class=\"ndb-dashboard\" style=\"margin-bottom: 16px;\">");
        sb.Append("<div class=\"ndb-card accent\">");
        sb.Append("<div class=\"ndb-card-label\">Total Logs</div>");
        sb.Append($"<div class=\"ndb-card-value\">{allLogs.Count}</div>");
        sb.Append("</div>");

        foreach (var group in byLevel)
        {
            var color = GetLogLevelColor(group.Key);
            sb.Append("<div class=\"ndb-card\">");
            sb.Append($"<div class=\"ndb-card-label\">{group.Key}</div>");
            sb.Append($"<div class=\"ndb-card-value\" style=\"color: {color};\">{group.Count()}</div>");
            sb.Append("</div>");
        }
        sb.Append("</div>");

        // Log level filter buttons
        sb.Append("<div style=\"margin-bottom: 12px;\">");
        foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
        {
            if (level == LogLevel.None) continue;
            var color = GetLogLevelColor(level);
            var count = allLogs.Count(l => l.Level == level);
            if (count > 0)
            {
                sb.Append($"<button class=\"ndb-log-filter active\" data-level=\"{level}\" style=\"background: {color}; border: none; color: #000; padding: 4px 8px; margin: 2px; border-radius: 3px; cursor: pointer; font-size: 10px; font-weight: bold;\">");
                sb.Append($"{level} ({count})");
                sb.Append("</button>");
            }
        }
        sb.Append("</div>");

        // Previous Logs
        if (hasPrevious)
        {
            sb.Append("<div class=\"ndb-section-header\">Previous Request</div>");
            foreach (var log in debug.PreviousSnapshot!.LogEntries)
            {
                RenderLogEntry(sb, log);
            }
        }

        // Current Logs
        if (hasCurrent)
        {
            sb.Append("<div class=\"ndb-section-header\">Current Request</div>");
            foreach (var log in debug.LogEntries)
            {
                RenderLogEntry(sb, log);
            }
        }

        return sb.ToString();
    }

    private void RenderLogEntry(StringBuilder sb, LogEntry log)
    {
        var color = GetLogLevelColor(log.Level);
        var levelBadge = GetLogLevelBadge(log.Level);

        sb.Append($"<div class=\"ndb-log-entry\" data-level=\"{log.Level}\" style=\"margin: 8px 0; padding: 8px; background: rgba(255,255,255,0.03); border-left: 3px solid {color}; border-radius: 2px;\">");

        // Header: timestamp, level, category
        sb.Append("<div style=\"display: flex; align-items: center; margin-bottom: 4px;\">");
        sb.Append($"<span style=\"color: #666; font-size: 10px; margin-right: 8px;\">{log.Timestamp:HH:mm:ss.fff}</span>");
        sb.Append($"<span style=\"background: {color}; color: #000; padding: 2px 6px; border-radius: 3px; font-size: 10px; font-weight: bold; margin-right: 8px;\">{levelBadge}</span>");
        sb.Append($"<span style=\"color: #999; font-size: 10px; font-family: 'Courier New', monospace;\">{Encode(log.Category)}</span>");
        sb.Append("</div>");

        // Message
        sb.Append($"<div style=\"color: #ddd; font-size: 11px; line-height: 1.4;\">{Encode(log.Message)}</div>");

        // Exception (if any)
        if (!string.IsNullOrEmpty(log.Exception))
        {
            sb.Append("<details style=\"margin-top: 4px;\">");
            sb.Append("<summary style=\"color: #ef4444; font-size: 10px; cursor: pointer;\">Exception Details</summary>");
            sb.Append($"<pre style=\"color: #ef4444; font-size: 10px; margin-top: 4px; white-space: pre-wrap; font-family: 'Courier New', monospace;\">{Encode(log.Exception)}</pre>");
            sb.Append("</details>");
        }

        sb.Append("</div>");
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
