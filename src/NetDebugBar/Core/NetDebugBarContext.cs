using System.Diagnostics;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Core;

public class NetDebugBarContext
{
    public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

    // Queries panel
    public List<QueryInfo> CurrentQueries { get; set; } = new();
    public List<QueryInfo>? PreviousQueries { get; set; }
    public List<NPlusOneGroup> NPlusOneGroups { get; set; } = new();

    // Request panel
    public RequestInfo? Request { get; set; }

    // Cache panel
    public List<CacheOperationInfo> CacheOperations { get; set; } = new();

    // Timeline panel
    public List<TimelineEvent> TimelineEvents { get; set; } = new();

    // Logging panel
    public List<LogEntry> LogEntries { get; set; } = new();

    // Models panel
    public ModelInfo? CurrentModels { get; set; }

    // For previous-request data (PRG pattern)
    public NetDebugBarContextSnapshot? PreviousSnapshot { get; set; }
}