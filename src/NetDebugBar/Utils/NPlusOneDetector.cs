using System.Text.RegularExpressions;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Utils;

/// <summary>
/// Detects potential N+1 query problems by analyzing query patterns.
/// </summary>
public static class NPlusOneDetector
{
    /// <summary>
    /// Analyzes a list of queries and identifies potential N+1 patterns.
    /// </summary>
    /// <param name="queries">The queries to analyze.</param>
    /// <param name="threshold">Minimum number of similar queries to flag as N+1 (default: 3).</param>
    /// <returns>List of N+1 query groups.</returns>
    public static List<NPlusOneGroup> DetectNPlusOne(IEnumerable<QueryInfo> queries, int threshold = 3)
    {
        var queryList = queries.ToList();
        if (queryList.Count < threshold)
            return new List<NPlusOneGroup>();

        // Group queries by normalized SQL
        var groups = queryList
            .GroupBy(q => NormalizeSql(q.Sql))
            .Where(g => g.Count() >= threshold)
            .Select(g => new NPlusOneGroup
            {
                NormalizedSql = g.Key,
                Queries = g.ToList()
            })
            .OrderByDescending(g => g.Count)
            .ToList();

        return groups;
    }

    /// <summary>
    /// Normalizes SQL by removing parameter values to identify similar query patterns.
    /// </summary>
    private static string NormalizeSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return "";

        var normalized = sql.Trim();

        // Replace string literals (single quotes)
        normalized = Regex.Replace(normalized, @"'[^']*'", "?", RegexOptions.Compiled);

        // Replace numeric literals (including decimals and negatives)
        normalized = Regex.Replace(normalized, @"\b-?\d+\.?\d*\b", "?", RegexOptions.Compiled);

        // Replace parameter markers (@p0, @p1, @param1, etc.)
        normalized = Regex.Replace(normalized, @"@\w+", "?", RegexOptions.Compiled);

        // Replace ? = ? patterns with single ?
        normalized = Regex.Replace(normalized, @"\?\s*=\s*\?", "? = ?", RegexOptions.Compiled);

        // Normalize whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ", RegexOptions.Compiled);

        return normalized;
    }
}
