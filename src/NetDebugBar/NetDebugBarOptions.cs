using Microsoft.Extensions.Logging;

namespace NetDebugBar;

public class NetDebugBarOptions
{
    public bool EnableQueryPanel { get; set; } = true;
    public bool EnableRequestPanel { get; set; } = true;
    public bool EnableCachePanel { get; set; } = true;
    public bool EnableTimelinePanel { get; set; } = true;
    public bool EnableLoggingPanel { get; set; } = true;
    public double SlowQueryThresholdMs { get; set; } = 150;
    public double MediumQueryThresholdMs { get; set; } = 50;
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Debug;
    public string AccentColor { get; set; } = "#a855f7";
    public bool EnableNPlusOneDetection { get; set; } = true;
    public int NPlusOneThreshold { get; set; } = 3;
}
