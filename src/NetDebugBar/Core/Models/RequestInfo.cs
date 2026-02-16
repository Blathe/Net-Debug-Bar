namespace NetDebugBar.Core.Models;

public class RequestInfo
{
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string? QueryString { get; set; }
    public string? RouteTemplate { get; set; }
    public int StatusCode { get; set; }
    public string? ContentType { get; set; }
    public long? ResponseSize { get; set; }
    public double DurationMs { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> Cookies { get; set; } = new();
    public Dictionary<string, string> QueryParameters { get; set; } = new();
}