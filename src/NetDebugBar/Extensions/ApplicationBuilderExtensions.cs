using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NetDebugBar.Middleware;
using NetDebugBar.Timeline;

namespace NetDebugBar.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Enables the Net Debug Bar to be displayed. This can expose sensitive data, so ensure you always wrap this in an appropriate environment check (e.g. only enable in Development).
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseNetDebugBar(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<NetDebugBarOptions>();

        // Timeline middleware: wraps everything to capture full request lifecycle
        if (options.EnableTimelinePanel)
        {
            app.UseMiddleware<TimelineMiddleware>();

            // Subscribe to diagnostic events
            var observer = app.ApplicationServices.GetRequiredService<TimelineDiagnosticObserver>();
            DiagnosticListener.AllListeners.Subscribe(observer);
        }

        // Core middleware: PRG cookie handling + context initialization
        app.UseMiddleware<DebugBarMiddleware>();

        // HTML injection middleware: must be last to wrap the full response
        // (also handles entity tracking before rendering)
        app.UseMiddleware<HtmlInjectionMiddleware>();

        return app;
    }
}
