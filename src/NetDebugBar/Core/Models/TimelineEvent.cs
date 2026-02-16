namespace NetDebugBar.Core.Models;

public class TimelineEvent
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public double StartOffsetMs { get; set; }
    public double DurationMs { get; set; }
    public string? Color { get; set; }
}