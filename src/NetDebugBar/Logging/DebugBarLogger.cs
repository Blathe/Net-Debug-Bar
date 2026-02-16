using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NetDebugBar.Core;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Logging;

public class DebugBarLogger : ILogger
{
    private readonly string _categoryName;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NetDebugBarOptions _options;

    public DebugBarLogger(string categoryName, IHttpContextAccessor httpContextAccessor, NetDebugBarOptions options)
    {
        _categoryName = categoryName;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _options.MinimumLogLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var context = _httpContextAccessor.HttpContext;
        if (context?.Items["NetDebugBarContext"] is not NetDebugBarContext debug)
            return;

        var entry = new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = logLevel,
            Category = _categoryName,
            Message = formatter(state, exception),
            Exception = exception?.ToString()
        };

        lock (debug.LogEntries)
        {
            debug.LogEntries.Add(entry);
        }
    }
}
