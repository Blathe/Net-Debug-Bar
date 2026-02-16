using NetDebugBar.Core.Models;

namespace NetDebugBar.Core;

public class NetDebugBarContextSnapshot
{
    public List<QueryInfo> Queries { get; set; } = new();
    public RequestInfo? Request { get; set; }
    public List<CacheOperationInfo> CacheOperations { get; set; } = new();
    public List<TimelineEvent> TimelineEvents { get; set; } = new();
    public List<LogEntry> LogEntries { get; set; } = new();
}