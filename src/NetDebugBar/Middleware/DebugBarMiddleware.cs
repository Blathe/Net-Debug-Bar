using Microsoft.AspNetCore.Http;
using NetDebugBar.Core;

namespace NetDebugBar.Middleware;

/// <summary>
/// Middleware that manages the debug context across requests,
/// particularly for POST-Redirect-GET patterns.
/// </summary>
public class DebugBarMiddleware
{
    private readonly RequestDelegate _next;
    private const string CookieName = "__NetDebugBarPrev";

    public DebugBarMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, NetDebugBarContext debug, DebugBarStorage storage)
    {
        // Phase 1: Restore previous request data from storage
        if (context.Request.Cookies.TryGetValue(CookieName, out var storageId))
        {
            var snapshot = storage.Retrieve(storageId);
            if (snapshot != null)
                debug.PreviousSnapshot = snapshot;
            context.Response.Cookies.Delete(CookieName);
        }

        // Phase 2: Register context for this request
        context.Items["NetDebugBarContext"] = debug;
        var requestMethod = context.Request.Method;

        await _next(context);

        // Phase 3: Save to storage on POST redirect
        if (IsPostRedirect(context, requestMethod))
        {
            var snapshot = CreateSnapshot(debug);
            var id = storage.Store(snapshot);
            context.Response.Cookies.Append(CookieName, id, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(1)
            });
        }
    }

    private static bool IsPostRedirect(HttpContext context, string requestMethod)
    {
        return HttpMethods.IsPost(requestMethod)
            && context.Response.StatusCode is >= 300 and < 400
            && context.Response.Headers.ContainsKey("Location");
    }

    private static NetDebugBarContextSnapshot CreateSnapshot(NetDebugBarContext debug)
    {
        return new NetDebugBarContextSnapshot
        {
            Queries = debug.CurrentQueries.ToList(),
            Request = debug.Request,
            CacheOperations = debug.CacheOperations.ToList(),
            TimelineEvents = debug.TimelineEvents.ToList(),
            LogEntries = debug.LogEntries.ToList(),
        };
    }
}
