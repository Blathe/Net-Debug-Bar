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
        var hasPrevious = debug.PreviousSnapshot?.TimelineEvents.Any() == true;
        var hasCurrent = debug.TimelineEvents.Any();

        if (!hasCurrent && !hasPrevious)
            return """<p style="color: #999; margin-top: 10px;">No timeline events recorded</p>""";

        var previousTimeline = hasPrevious
            ? RenderPreviousTimeline(debug.PreviousSnapshot!.TimelineEvents, debug.PreviousSnapshot.Request?.DurationMs ?? 100)
            : "";

        var currentTimeline = hasCurrent
            ? RenderCurrentTimeline(debug.TimelineEvents, debug.Request?.DurationMs ?? debug.Stopwatch.Elapsed.TotalMilliseconds)
            : "";

        return $$"""
            {{previousTimeline}}
            {{currentTimeline}}
            """;
    }

    private string RenderPreviousTimeline(List<TimelineEvent> events, double totalDuration)
    {
        var timeline = RenderTimeline(events, totalDuration);
        return $$"""
            <div class="ndb-section-header">Previous Request</div>
            {{timeline}}
            """;
    }

    private string RenderCurrentTimeline(List<TimelineEvent> events, double totalDuration)
    {
        var timeline = RenderTimeline(events, totalDuration);
        return $$"""
            <div class="ndb-section-header">Current Request</div>
            {{timeline}}
            """;
    }

    private string RenderTimeline(List<TimelineEvent> events, double totalDuration)
    {
        if (totalDuration <= 0) totalDuration = 1; // Avoid division by zero

        var sortedEvents = events.OrderBy(e => e.StartOffsetMs);
        var eventRows = string.Join("", sortedEvents.Select(evt => RenderTimelineEvent(evt, totalDuration)));

        return $$"""
            <div class="ndb-timeline">
                {{eventRows}}
                <div class="ndb-timeline-row" style="border-top: 1px solid #555; margin-top: 8px; padding-top: 8px;">
                    <div class="ndb-timeline-label" style="font-weight: bold; color: var(--ndb-accent-color);">Total Duration</div>
                    <div class="ndb-timeline-track"></div>
                    <div class="ndb-timeline-time" style="font-weight: bold; color: var(--ndb-accent-color);">{{totalDuration:0.##}}ms</div>
                </div>
            </div>
            """;
    }

    private string RenderTimelineEvent(TimelineEvent evt, double totalDuration)
    {
        var leftPercent = (evt.StartOffsetMs / totalDuration) * 100;
        var widthPercent = Math.Max((evt.DurationMs / totalDuration) * 100, 0.5); // Min 0.5% width for visibility
        var color = evt.Color ?? "#6366f1";

        return $$"""
            <div class="ndb-timeline-row">
                <div class="ndb-timeline-label">
                    <span style="color: {{color}};">●</span> {{Encode(evt.Name)}}
                </div>
                <div class="ndb-timeline-track">
                    <div class="ndb-timeline-bar" style="left:{{leftPercent:0.##}}%;width:{{widthPercent:0.##}}%;background:{{color}};" title="{{Encode(evt.Name)}}: {{evt.DurationMs:0.##}}ms"></div>
                </div>
                <div class="ndb-timeline-time">{{evt.DurationMs:0.##}}ms</div>
            </div>
            """;
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
}
