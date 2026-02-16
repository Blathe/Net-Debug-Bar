using System.Text;
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
        var sb = new StringBuilder();
        var req = debug.Request;

        if (req == null)
        {
            sb.Append("<p style=\"color: #999;\">No request info available</p>");
            return sb.ToString();
        }

        // Summary cards
        sb.Append("<div class=\"ndb-dashboard\" style=\"margin-bottom: 16px;\">");
        RenderCard(sb, "Method", req.Method, GetMethodColor(req.Method));
        RenderCard(sb, "Status", req.StatusCode.ToString(), GetStatusColor(req.StatusCode));
        RenderCard(sb, "Duration", $"{req.DurationMs:0.##}ms");
        if (req.ResponseSize.HasValue)
        {
            RenderCard(sb, "Response Size", FormatBytes(req.ResponseSize.Value));
        }
        sb.Append("</div>");

        // Path and Route
        sb.Append("<div class=\"ndb-info-section\">");
        sb.Append($"<div class=\"ndb-info-label\">Path</div>");
        sb.Append($"<div class=\"ndb-info-value\">{Encode(req.Path)}</div>");
        sb.Append("</div>");

        if (!string.IsNullOrEmpty(req.QueryString))
        {
            sb.Append("<div class=\"ndb-info-section\">");
            sb.Append($"<div class=\"ndb-info-label\">Query String</div>");
            sb.Append($"<div class=\"ndb-info-value\">{Encode(req.QueryString)}</div>");
            sb.Append("</div>");
        }

        if (!string.IsNullOrEmpty(req.RouteTemplate))
        {
            sb.Append("<div class=\"ndb-info-section\">");
            sb.Append($"<div class=\"ndb-info-label\">Route Template</div>");
            sb.Append($"<div class=\"ndb-info-value\">{Encode(req.RouteTemplate)}</div>");
            sb.Append("</div>");
        }

        if (!string.IsNullOrEmpty(req.ContentType))
        {
            sb.Append("<div class=\"ndb-info-section\">");
            sb.Append($"<div class=\"ndb-info-label\">Content Type</div>");
            sb.Append($"<div class=\"ndb-info-value\">{Encode(req.ContentType)}</div>");
            sb.Append("</div>");
        }

        // Query Parameters (expandable)
        if (req.QueryParameters.Any())
        {
            sb.Append("<details class=\"ndb-expandable\" open>");
            sb.Append($"<summary>Query Parameters ({req.QueryParameters.Count})</summary>");
            sb.Append("<table class=\"ndb-table\">");
            foreach (var param in req.QueryParameters)
            {
                sb.Append("<tr>");
                sb.Append($"<td class=\"ndb-table-key\">{Encode(param.Key)}</td>");
                sb.Append($"<td class=\"ndb-table-value\">{Encode(param.Value)}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            sb.Append("</details>");
        }

        // Headers (expandable)
        if (req.Headers.Any())
        {
            sb.Append("<details class=\"ndb-expandable\">");
            sb.Append($"<summary>Headers ({req.Headers.Count})</summary>");
            sb.Append("<table class=\"ndb-table\">");
            foreach (var header in req.Headers.OrderBy(h => h.Key))
            {
                sb.Append("<tr>");
                sb.Append($"<td class=\"ndb-table-key\">{Encode(header.Key)}</td>");
                sb.Append($"<td class=\"ndb-table-value\">{Encode(header.Value)}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            sb.Append("</details>");
        }

        // Cookies (expandable)
        if (req.Cookies.Any())
        {
            sb.Append("<details class=\"ndb-expandable\">");
            sb.Append($"<summary>Cookies ({req.Cookies.Count})</summary>");
            sb.Append("<table class=\"ndb-table\">");
            foreach (var cookie in req.Cookies.OrderBy(c => c.Key))
            {
                sb.Append("<tr>");
                sb.Append($"<td class=\"ndb-table-key\">{Encode(cookie.Key)}</td>");
                sb.Append($"<td class=\"ndb-table-value\">{Encode(cookie.Value)}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            sb.Append("</details>");
        }

        return sb.ToString();
    }

    private void RenderCard(StringBuilder sb, string label, string value, string? color = null)
    {
        sb.Append("<div class=\"ndb-card\">");
        sb.Append($"<div class=\"ndb-card-label\">{Encode(label)}</div>");
        if (color != null)
        {
            sb.Append($"<div class=\"ndb-card-value\" style=\"color: {color};\">{Encode(value)}</div>");
        }
        else
        {
            sb.Append($"<div class=\"ndb-card-value\">{Encode(value)}</div>");
        }
        sb.Append("</div>");
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
