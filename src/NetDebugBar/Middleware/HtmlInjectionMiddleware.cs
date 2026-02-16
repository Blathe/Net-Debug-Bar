using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NetDebugBar.Core;
using NetDebugBar.Core.Models;
using NetDebugBar.Rendering;

namespace NetDebugBar.Middleware;

/// <summary>
/// Middleware that injects the debug bar HTML before the closing body tag.
/// Buffers the response to enable HTML injection without requiring layout modification.
/// </summary>
public class HtmlInjectionMiddleware
{
    private readonly RequestDelegate _next;

    public HtmlInjectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, NetDebugBarContext debug, NetDebugBarOptions options)
    {
        var originalBody = context.Response.Body;
        using var bufferStream = new MemoryStream();
        context.Response.Body = bufferStream;

        try
        {
            await _next(context);

            // Check if response is HTML
            var contentType = context.Response.ContentType;
            if (contentType != null && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                // Stop the stopwatch to get accurate timing
                debug.Stopwatch.Stop();

                // Populate request info
                PopulateRequestInfo(context, debug, bufferStream.Length);

                // Generate debug bar HTML
                var renderer = new DebugBarHtmlRenderer(options);
                var debugBarHtml = renderer.Render(debug);

                // Read buffered response
                bufferStream.Seek(0, SeekOrigin.Begin);
                var html = await new StreamReader(bufferStream).ReadToEndAsync();

                // Inject before </body>
                var bodyCloseIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
                if (bodyCloseIndex >= 0)
                {
                    html = html.Insert(bodyCloseIndex, debugBarHtml);
                }
                else
                {
                    // No </body> tag -- append at end
                    html += debugBarHtml;
                }

                // Write modified response
                var bytes = Encoding.UTF8.GetBytes(html);
                context.Response.ContentLength = bytes.Length;
                context.Response.Body = originalBody;
                await originalBody.WriteAsync(bytes);
            }
            else
            {
                // Not HTML -- write buffer to original stream unchanged
                bufferStream.Seek(0, SeekOrigin.Begin);
                context.Response.Body = originalBody;
                await bufferStream.CopyToAsync(originalBody);
            }
        }
        catch
        {
            context.Response.Body = originalBody;
            throw;
        }
    }

    private void PopulateRequestInfo(HttpContext context, NetDebugBarContext debug, long responseSize)
    {
        debug.Request = new RequestInfo
        {
            Method = context.Request.Method,
            Path = context.Request.Path.Value ?? "",
            QueryString = context.Request.QueryString.Value,
            StatusCode = context.Response.StatusCode,
            ContentType = context.Response.ContentType,
            ResponseSize = responseSize,
            DurationMs = debug.Stopwatch.Elapsed.TotalMilliseconds,
            RouteTemplate = GetRouteTemplate(context),
        };

        // Headers
        foreach (var header in context.Request.Headers)
            debug.Request.Headers[header.Key] = header.Value.ToString();

        // Cookies
        foreach (var cookie in context.Request.Cookies)
            debug.Request.Cookies[cookie.Key] = cookie.Value;

        // Query parameters
        foreach (var param in context.Request.Query)
            debug.Request.QueryParameters[param.Key] = param.Value.ToString();
    }

    private string? GetRouteTemplate(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is RouteEndpoint routeEndpoint)
            return routeEndpoint.RoutePattern.RawText;
        return null;
    }
}
