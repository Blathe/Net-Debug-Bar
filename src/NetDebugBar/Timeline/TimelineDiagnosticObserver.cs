using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using NetDebugBar.Core;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Timeline;

/// <summary>
/// Observes DiagnosticListener events from ASP.NET Core to capture request lifecycle timing.
/// </summary>
public class TimelineDiagnosticObserver : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ConcurrentDictionary<string, TimelineEvent> _pendingEvents = new();

    public TimelineDiagnosticObserver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == "Microsoft.AspNetCore")
        {
            listener.Subscribe(this);
        }
    }

    public void OnNext(KeyValuePair<string, object?> pair)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items["NetDebugBarContext"] is not NetDebugBarContext debug)
            return;

        var offsetMs = debug.Stopwatch.Elapsed.TotalMilliseconds;

        switch (pair.Key)
        {
            case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                AddEvent(debug, "HTTP Request Start", "Hosting", offsetMs, "#10b981");
                break;

            case "Microsoft.AspNetCore.Routing.EndpointMatched":
                AddEvent(debug, "Endpoint Matched", "Routing", offsetMs, "#22c55e");
                break;

            case "Microsoft.AspNetCore.Mvc.BeforeOnAuthorization":
                StartEvent(context, "Authorization", "Authorization", "Auth", offsetMs, "#f59e0b");
                break;

            case "Microsoft.AspNetCore.Mvc.AfterOnAuthorization":
                CompleteEvent(context, "Authorization", offsetMs);
                break;

            case "Microsoft.AspNetCore.Mvc.BeforeOnPageHandlerExecution":
            case "Microsoft.AspNetCore.Mvc.BeforeActionExecution":
                StartEvent(context, "Handler", "Handler Execution", "Handler", offsetMs, "#3b82f6");
                break;

            case "Microsoft.AspNetCore.Mvc.AfterOnPageHandlerExecution":
            case "Microsoft.AspNetCore.Mvc.AfterActionExecution":
                CompleteEvent(context, "Handler", offsetMs);
                break;

            case "Microsoft.AspNetCore.Mvc.BeforeOnResultExecution":
                StartEvent(context, "Result", "Result Execution", "Result", offsetMs, "#8b5cf6");
                break;

            case "Microsoft.AspNetCore.Mvc.AfterOnResultExecution":
                CompleteEvent(context, "Result", offsetMs);
                break;
        }
    }

    public void OnCompleted() { }
    public void OnError(Exception error) { }

    private void AddEvent(NetDebugBarContext debug, string name, string category, double offsetMs, string color)
    {
        var evt = new TimelineEvent
        {
            Name = name,
            Category = category,
            StartOffsetMs = offsetMs,
            DurationMs = 0.1, // Small duration for instant events
            Color = color
        };

        lock (debug.TimelineEvents)
        {
            debug.TimelineEvents.Add(evt);
        }
    }

    private void StartEvent(HttpContext context, string key, string name, string category, double offsetMs, string color)
    {
        var evt = new TimelineEvent
        {
            Name = name,
            Category = category,
            StartOffsetMs = offsetMs,
            Color = color
        };

        var pendingKey = $"{context.TraceIdentifier}_{key}";
        _pendingEvents[pendingKey] = evt;
    }

    private void CompleteEvent(HttpContext context, string key, double endOffsetMs)
    {
        var pendingKey = $"{context.TraceIdentifier}_{key}";
        if (_pendingEvents.TryRemove(pendingKey, out var evt))
        {
            evt.DurationMs = endOffsetMs - evt.StartOffsetMs;

            if (context.Items["NetDebugBarContext"] is NetDebugBarContext debug)
            {
                lock (debug.TimelineEvents)
                {
                    debug.TimelineEvents.Add(evt);
                }
            }
        }
    }
}
