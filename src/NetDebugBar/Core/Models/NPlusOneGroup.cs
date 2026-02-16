namespace NetDebugBar.Core.Models;

/// <summary>
/// Represents a group of similar queries that indicate a potential N+1 query problem.
/// </summary>
public class NPlusOneGroup
{
    /// <summary>
    /// The normalized SQL pattern (with parameter placeholders removed).
    /// </summary>
    public string NormalizedSql { get; set; } = "";

    /// <summary>
    /// The queries in this group.
    /// </summary>
    public List<QueryInfo> Queries { get; set; } = new();

    /// <summary>
    /// Total duration of all queries in this group.
    /// </summary>
    public double TotalDuration => Queries.Sum(q => q.Duration);

    /// <summary>
    /// Number of queries in this group.
    /// </summary>
    public int Count => Queries.Count;
}
