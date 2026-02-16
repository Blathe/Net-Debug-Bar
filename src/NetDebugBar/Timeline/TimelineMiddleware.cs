using Microsoft.AspNetCore.Http;
using NetDebugBar.Core;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Timeline;

/// <summary>
/// Middleware that wraps the entire request pipeline to capture timing information.
/// </summary>
public class TimelineMiddleware
{
    private readonly RequestDelegate _next;

    public TimelineMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, NetDebugBarContext debug)
    {
        var startOffset = debug.Stopwatch.Elapsed.TotalMilliseconds;

        // Record request start event
        var requestEvent = new TimelineEvent
        {
            Name = "Request Pipeline",
            Category = "Pipeline",
            StartOffsetMs = startOffset,
            Color = "#6366f1"
        };

        lock (debug.TimelineEvents)
        {
            debug.TimelineEvents.Add(requestEvent);
        }

        await _next(context);

        // Update with total duration
        var totalMs = debug.Stopwatch.Elapsed.TotalMilliseconds;
        requestEvent.DurationMs = totalMs - startOffset;
    }
}
