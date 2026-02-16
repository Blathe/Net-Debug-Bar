using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Diagnostics;
using NetDebugBar.Core;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Interceptors;

/// <summary>
/// Intercepts EF Core database commands to capture SQL queries and parameters for debugging.
/// Handles batched SQL statements and records each separately.
/// </summary>
public class NetDebugBarQueryInterceptor : DbCommandInterceptor
{
    private readonly NetDebugBarContext _debug;
    private readonly Dictionary<DbCommand, Stopwatch> _timings = new();

    public NetDebugBarQueryInterceptor(NetDebugBarContext debug)
    {
        _debug = debug;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        StartTiming(command, command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StartTiming(command, command.CommandText);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        StartTiming(command, command.CommandText);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        var res = await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        RecordQuery(command);
        return res;
    }

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var res = await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        RecordQuery(command);
        return res;
    }

    public override async ValueTask<object> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        var res = await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        RecordQuery(command);
        return res;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        var res = base.ReaderExecuted(command, eventData, result);
        RecordQuery(command);
        return res;
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        var res = base.NonQueryExecuted(command, eventData, result);
        RecordQuery(command);
        return res;
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        var res = base.ScalarExecuted(command, eventData, result);
        RecordQuery(command);
        return res;
    }

    private void StartTiming(DbCommand command, string sql)
    {
        if (ContainsTrackableStatement(sql))
        {
            _timings[command] = Stopwatch.StartNew();
        }
    }

    /// <summary>
    /// Checks if SQL batch contains any trackable statements (not just SET/DECLARE)
    /// </summary>
    private static bool ContainsTrackableStatement(string sql)
    {
        return sql.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            .Any(line =>
            {
                var trimmed = line.Trim();
                return !string.IsNullOrEmpty(trimmed) &&
                       !trimmed.StartsWith("SET ", StringComparison.OrdinalIgnoreCase) &&
                       !trimmed.StartsWith("DECLARE ", StringComparison.OrdinalIgnoreCase);
            });
    }

    /// <summary>
    /// Records SQL statements from a command, handling batched statements.
    /// </summary>
    private void RecordQuery(DbCommand command)
    {
        var fullSql = command.CommandText.Trim();
        double totalDurationMs = 0;

        // Get total duration if timing was captured
        if (_timings.TryGetValue(command, out var sw))
        {
            sw.Stop();
            totalDurationMs = sw.Elapsed.TotalMilliseconds;
            _timings.Remove(command);
        }

        // Capture parameters
        var parameters = CaptureParameters(command);

        // Parse and record each statement
        var statements = ExtractStatements(fullSql);

        foreach (var sql in statements)
        {
            if (!IsTrackableStatement(sql))
                continue;

            var query = new QueryInfo
            {
                Sql = sql,
                Duration = totalDurationMs / statements.Count, // Divide duration among statements
                Parameters = parameters
            };

            lock (_debug.CurrentQueries)
            {
                _debug.CurrentQueries.Add(query);
            }
        }
    }

    /// <summary>
    /// Captures parameter names, values, and types from the command.
    /// </summary>
    private static List<QueryParameterInfo> CaptureParameters(DbCommand command)
    {
        var parameters = new List<QueryParameterInfo>();
        foreach (DbParameter param in command.Parameters)
        {
            parameters.Add(new QueryParameterInfo
            {
                Name = param.ParameterName,
                Value = param.Value == DBNull.Value ? null : param.Value?.ToString(),
                TypeName = param.DbType.ToString()
            });
        }
        return parameters;
    }

    /// <summary>
    /// Extracts individual SQL statements from a potentially batched SQL command.
    /// </summary>
    private static List<string> ExtractStatements(string fullSql)
    {
        var statements = new List<string>();
        var currentStatement = new List<string>();

        foreach (var line in fullSql.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
        {
            var trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed))
                continue;

            currentStatement.Add(line);

            // Check for statement boundaries
            if (IsStatementEnd(trimmed))
            {
                var sql = string.Join(Environment.NewLine, currentStatement).Trim();
                if (!string.IsNullOrEmpty(sql))
                {
                    statements.Add(sql);
                }
                currentStatement.Clear();
            }
        }

        // Add any remaining statement
        if (currentStatement.Count > 0)
        {
            var sql = string.Join(Environment.NewLine, currentStatement).Trim();
            if (!string.IsNullOrEmpty(sql))
            {
                statements.Add(sql);
            }
        }

        return statements.Count > 0 ? statements : new List<string> { fullSql };
    }

    /// <summary>
    /// Determines if a line indicates the end of a SQL statement.
    /// </summary>
    private static bool IsStatementEnd(string trimmedLine)
    {
        return trimmedLine.EndsWith(";") ||
               trimmedLine.EndsWith(");") ||
               trimmedLine.Equals("GO", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a statement should be tracked (not a system setup command).
    /// </summary>
    private static bool IsTrackableStatement(string sql)
    {
        var firstLine = sql.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            .FirstOrDefault(l => !string.IsNullOrEmpty(l.Trim()))?.Trim() ?? "";

        return !firstLine.StartsWith("SET ", StringComparison.OrdinalIgnoreCase) &&
               !firstLine.StartsWith("DECLARE ", StringComparison.OrdinalIgnoreCase);
    }
}
