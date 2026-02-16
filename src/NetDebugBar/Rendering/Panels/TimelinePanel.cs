using System.Text;
using NetDebugBar.Core;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Rendering.Panels;

public class TimelinePanel : IDebugBarPanel
{
    public string TabId => "timeline";
    public string TabLabel => "Timeline";

    public string? RenderBadge(NetDebugBarContext debug)
    {
        var total = debug.TimelineEvents.Count + (debug.PreviousSnapshot?.TimelineEvents.Count ?? 0);
        return total > 0 ? total.ToString() : null;
    }

    public string RenderTabContent(NetDebugBarContext debug)
    {
        var sb = new StringBuilder();

        var hasPrevious = debug.PreviousSnapshot?.TimelineEvents.Any() == true;
        var hasCurrent = debug.TimelineEvents.Any();

        if (!hasCurrent && !hasPrevious)
        {
            sb.Append("<p style=\"color: #999; margin-top: 10px;\">No timeline events recorded</p>");
            return sb.ToString();
        }

        // Previous Request Timeline
        if (hasPrevious)
        {
            sb.Append("<div class=\"ndb-section-header\">Previous Request</div>");
            RenderTimeline(sb, debug.PreviousSnapshot!.TimelineEvents, debug.PreviousSnapshot.Request?.DurationMs ?? 100);
        }

        // Current Request Timeline
        if (hasCurrent)
        {
            sb.Append("<div class=\"ndb-section-header\">Current Request</div>");
            var totalDuration = debug.Request?.DurationMs ?? debug.Stopwatch.Elapsed.TotalMilliseconds;
            RenderTimeline(sb, debug.TimelineEvents, totalDuration);
        }

        return sb.ToString();
    }

    private void RenderTimeline(StringBuilder sb, List<TimelineEvent> events, double totalDuration)
    {
        if (totalDuration <= 0) totalDuration = 1; // Avoid division by zero

        // Sort events by start time
        var sortedEvents = events.OrderBy(e => e.StartOffsetMs).ToList();

        sb.Append("<div class=\"ndb-timeline\">");

        foreach (var evt in sortedEvents)
        {
            var leftPercent = (evt.StartOffsetMs / totalDuration) * 100;
            var widthPercent = Math.Max((evt.DurationMs / totalDuration) * 100, 0.5); // Min 0.5% width for visibility
            var color = evt.Color ?? "#6366f1";

            sb.Append("<div class=\"ndb-timeline-row\">");

            // Event label
            sb.Append($"<div class=\"ndb-timeline-label\">");
            sb.Append($"<span style=\"color: {color};\">●</span> ");
            sb.Append($"{Encode(evt.Name)}");
            sb.Append("</div>");

            // Timeline track with bar
            sb.Append("<div class=\"ndb-timeline-track\">");
            sb.Append($"<div class=\"ndb-timeline-bar\" style=\"left:{leftPercent:0.##}%;width:{widthPercent:0.##}%;background:{color};\" title=\"{Encode(evt.Name)}: {evt.DurationMs:0.##}ms\"></div>");
            sb.Append("</div>");

            // Duration
            sb.Append($"<div class=\"ndb-timeline-time\">{evt.DurationMs:0.##}ms</div>");

            sb.Append("</div>");
        }

        // Total duration indicator
        sb.Append("<div class=\"ndb-timeline-row\" style=\"border-top: 1px solid #555; margin-top: 8px; padding-top: 8px;\">");
        sb.Append("<div class=\"ndb-timeline-label\" style=\"font-weight: bold; color: #0ff;\">Total Duration</div>");
        sb.Append("<div class=\"ndb-timeline-track\"></div>");
        sb.Append($"<div class=\"ndb-timeline-time\" style=\"font-weight: bold; color: #0ff;\">{totalDuration:0.##}ms</div>");
        sb.Append("</div>");

        sb.Append("</div>");
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
}
