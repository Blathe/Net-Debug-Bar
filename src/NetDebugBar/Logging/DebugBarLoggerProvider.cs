using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NetDebugBar.Logging;

[ProviderAlias("NetDebugBar")]
public class DebugBarLoggerProvider : ILoggerProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NetDebugBarOptions _options;
    private readonly ConcurrentDictionary<string, DebugBarLogger> _loggers = new();

    public DebugBarLoggerProvider(IHttpContextAccessor httpContextAccessor, NetDebugBarOptions options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new DebugBarLogger(name, _httpContextAccessor, _options));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
