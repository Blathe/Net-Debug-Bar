using NetDebugBar.Core;

namespace NetDebugBar.Rendering.Panels;

public class RequestPanel : IDebugBarPanel
{
    public string TabId => "request";
    public string TabLabel => "Request";

    public string? RenderBadge(NetDebugBarContext debug)
    {
        return debug.Request?.StatusCode.ToString();
    }

    public string RenderTabContent(NetDebugBarContext debug)
    {
        var req = debug.Request;

        if (req == null)
            return """<p style="color: #999;">No request info available</p>""";

        var cards = RenderCards(req);
        var pathInfo = RenderPathInfo(req);
        var queryParams = RenderQueryParameters(req);
        var headers = RenderHeaders(req);
        var cookies = RenderCookies(req);

        return $$"""
            {{cards}}
            {{pathInfo}}
            {{queryParams}}
            {{headers}}
            {{cookies}}
            """;
    }

    private string RenderCards(Core.Models.RequestInfo req)
    {
        var responseSize = req.ResponseSize.HasValue ? RenderCard("Response Size", FormatBytes(req.ResponseSize.Value)) : "";

        return $$"""
            <div class="ndb-dashboard" style="margin-bottom: 16px;">
                {{RenderCard("Method", req.Method, GetMethodColor(req.Method))}}
                {{RenderCard("Status", req.StatusCode.ToString(), GetStatusColor(req.StatusCode))}}
                {{RenderCard("Duration", $"{req.DurationMs:0.##}ms")}}
                {{responseSize}}
            </div>
            """;
    }

    private string RenderPathInfo(Core.Models.RequestInfo req)
    {
        var queryString = !string.IsNullOrEmpty(req.QueryString) ? $$"""
            <div class="ndb-info-section">
                <div class="ndb-info-label">Query String</div>
                <div class="ndb-info-value">{{Encode(req.QueryString)}}</div>
            </div>
            """ : "";

        var routeTemplate = !string.IsNullOrEmpty(req.RouteTemplate) ? $$"""
            <div class="ndb-info-section">
                <div class="ndb-info-label">Route Template</div>
                <div class="ndb-info-value">{{Encode(req.RouteTemplate)}}</div>
            </div>
            """ : "";

        var contentType = !string.IsNullOrEmpty(req.ContentType) ? $$"""
            <div class="ndb-info-section">
                <div class="ndb-info-label">Content Type</div>
                <div class="ndb-info-value">{{Encode(req.ContentType)}}</div>
            </div>
            """ : "";

        return $$"""
            <div class="ndb-info-section">
                <div class="ndb-info-label">Path</div>
                <div class="ndb-info-value">{{Encode(req.Path)}}</div>
            </div>
            {{queryString}}
            {{routeTemplate}}
            {{contentType}}
            """;
    }

    private string RenderQueryParameters(Core.Models.RequestInfo req)
    {
        if (!req.QueryParameters.Any())
            return "";

        var rows = string.Join("", req.QueryParameters.Select(param => $$"""
            <tr>
                <td class="ndb-table-key">{{Encode(param.Key)}}</td>
                <td class="ndb-table-value">{{Encode(param.Value)}}</td>
            </tr>
            """));

        return $$"""
            <details class="ndb-expandable" open>
                <summary>Query Parameters ({{req.QueryParameters.Count}})</summary>
                <table class="ndb-table">
                    {{rows}}
                </table>
            </details>
            """;
    }

    private string RenderHeaders(Core.Models.RequestInfo req)
    {
        if (!req.Headers.Any())
            return "";

        var rows = string.Join("", req.Headers.OrderBy(h => h.Key).Select(header => $$"""
            <tr>
                <td class="ndb-table-key">{{Encode(header.Key)}}</td>
                <td class="ndb-table-value">{{Encode(header.Value)}}</td>
            </tr>
            """));

        return $$"""
            <details class="ndb-expandable">
                <summary>Headers ({{req.Headers.Count}})</summary>
                <table class="ndb-table">
                    {{rows}}
                </table>
            </details>
            """;
    }

    private string RenderCookies(Core.Models.RequestInfo req)
    {
        if (!req.Cookies.Any())
            return "";

        var rows = string.Join("", req.Cookies.OrderBy(c => c.Key).Select(cookie => $$"""
            <tr>
                <td class="ndb-table-key">{{Encode(cookie.Key)}}</td>
                <td class="ndb-table-value">{{Encode(cookie.Value)}}</td>
            </tr>
            """));

        return $$"""
            <details class="ndb-expandable">
                <summary>Cookies ({{req.Cookies.Count}})</summary>
                <table class="ndb-table">
                    {{rows}}
                </table>
            </details>
            """;
    }

    private string RenderCard(string label, string value, string? color = null)
    {
        var valueStyle = color != null ? $" style=\"color: {color};\"" : "";

        return $$"""
            <div class="ndb-card">
                <div class="ndb-card-label">{{Encode(label)}}</div>
                <div class="ndb-card-value"{{valueStyle}}>{{Encode(value)}}</div>
            </div>
            """;
    }

    private string? GetMethodColor(string method)
    {
        return method.ToUpper() switch
        {
            "GET" => "#3b82f6",    // blue
            "POST" => "#10b981",   // green
            "PUT" => "#f59e0b",    // amber
            "DELETE" => "#ef4444", // red
            "PATCH" => "#8b5cf6",  // purple
            _ => null
        };
    }

    private string? GetStatusColor(int statusCode)
    {
        if (statusCode >= 200 && statusCode < 300)
            return "#10b981"; // green
        if (statusCode >= 300 && statusCode < 400)
            return "#3b82f6"; // blue
        if (statusCode >= 400 && statusCode < 500)
            return "#f59e0b"; // amber
        if (statusCode >= 500)
            return "#ef4444"; // red
        return null;
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
}
