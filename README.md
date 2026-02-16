# NetDebugBar

A comprehensive debug toolbar for ASP.NET Core 9 applications, inspired by Laravel Debug Bar. NetDebugBar provides real-time insights into database queries, cache operations, logs, HTTP requests, and request timeline visualization.

![.NET Version](https://img.shields.io/badge/.NET-9.0-purple)
![License](https://img.shields.io/badge/license-MIT-blue)

## Features

- **Database Query Tracking** - Captures all Entity Framework Core queries with execution time, parameters, and performance color-coding
- **Cache Operations** - Monitors IMemoryCache operations (Get/Set/Remove) with hit/miss tracking and statistics
- **Logging Panel** - Displays all log entries with filtering by log level (Debug, Info, Warning, Error, Critical)
- **Request Information** - Shows HTTP method, path, status code, headers, cookies, and request duration
- **Timeline Visualization** - Visual timeline of request pipeline phases (routing, authorization, middleware, etc.)
- **POST-Redirect-GET Support** - Preserves debug data across redirects to show both previous and current request information
- **Zero Configuration** - No layout modifications required; automatically injects into HTML responses

## Installation

```bash
dotnet add package NetDebugBar
```

## Quick Start

### 1. Register Services

In your `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add NetDebugBar services
builder.Services.AddNetDebugBar();

// If using Entity Framework Core, register the interceptor
builder.Services.AddDbContext<MyDbContext>((serviceProvider, options) =>
{
    var interceptor = serviceProvider.GetRequiredService<NetDebugBarQueryInterceptor>();
    options.UseSqlServer(connectionString)
           .AddInterceptors(interceptor);
});
```

### 2. Add Middleware

```csharp
var app = builder.Build();

// Use NetDebugBar (only in development!)
if (app.Environment.IsDevelopment())
{
    app.UseNetDebugBar();
}

app.Run();
```

That's it! The debug bar will automatically appear at the bottom of your HTML pages.

## Configuration

Customize NetDebugBar by passing options to `AddNetDebugBar()`:

```csharp
builder.Services.AddNetDebugBar(options =>
{
    // Enable/disable specific panels
    options.EnableQueryPanel = true;
    options.EnableCachePanel = true;
    options.EnableLoggingPanel = true;
    options.EnableRequestPanel = true;
    options.EnableTimelinePanel = true;

    // Query performance thresholds
    options.SlowQueryThresholdMs = 150;      // Red color
    options.MediumQueryThresholdMs = 50;     // Yellow color

    // Logging configuration
    options.MinimumLogLevel = LogLevel.Debug;

    // UI customization
    options.AccentColor = "#a855f7";  // Purple accent color
});
```

## Panels Overview

### Overview Panel
Displays aggregate statistics across all panels:
- Total database queries and duration
- Current vs. previous request metrics
- Cache operation counts
- Log entry counts
- Request duration

### Queries Panel
Shows all Entity Framework Core queries:
- SQL command text
- Execution duration with color-coding (green < 50ms, yellow < 150ms, red ≥ 150ms)
- Parameters with values and types
- Separate sections for current and previous requests (if POST-Redirect-GET)

### Cache Panel
Monitors `IMemoryCache` operations:
- Operation type (Get/Set/Remove)
- Cache keys
- Hit/Miss status
- Hit rate percentage
- Value types
- Operation duration

### Logging Panel
Captures application logs:
- Log level filtering (Debug, Info, Warning, Error, Critical)
- Message text
- Category names
- Exception details with stack traces
- Timestamp information

### Request Panel
HTTP request details:
- Method, path, protocol
- Status code and duration
- Query string parameters
- Request headers
- Response headers
- Cookies
- Form data (for POST requests)

### Timeline Panel
Visual representation of request pipeline:
- Routing phase
- Authorization
- MVC action execution
- Middleware timing
- Total request duration
- Color-coded timeline bars

## How It Works

NetDebugBar uses several techniques to capture debug data:

1. **EF Core Interceptor** (`DbCommandInterceptor`) - Captures SQL queries and parameters
2. **Decorator Pattern** (`IMemoryCache` wrapper) - Intercepts cache operations
3. **Logger Provider** (`ILoggerProvider`) - Captures log entries
4. **Diagnostic Listener** - Subscribes to ASP.NET Core diagnostic events for timeline
5. **Response Buffering Middleware** - Injects HTML into responses before `</body>` tag

The middleware automatically:
- Detects HTML responses (`text/html` content type)
- Generates debug bar HTML with inline CSS/JavaScript
- Injects before closing `</body>` tag
- Handles POST-Redirect-GET pattern with cookie-based storage

## Requirements

- .NET 9.0 or later
- ASP.NET Core 9.0 or later
- Entity Framework Core 9.0 (optional, for query tracking)

## Development

### Build

```bash
dotnet build
```

### Run Demo Application

```bash
dotnet run --project samples/NetDebugBar.Demo
```

Then navigate to `http://localhost:5000` to see NetDebugBar in action.

### Project Structure

```
NetDebugBar/
├── src/NetDebugBar/              # Main library
│   ├── Core/                     # Core models and storage
│   ├── Middleware/               # Request pipeline middleware
│   ├── Rendering/                # HTML rendering and panels
│   │   └── Panels/              # Individual panel implementations
│   ├── Logging/                  # Logger provider
│   ├── Interceptors/             # EF Core query interceptor
│   └── Cache/                    # Memory cache decorator
└── samples/NetDebugBar.Demo/     # Demo ASP.NET Core application
```

## Important Notes

⚠️ **Development Only** - NetDebugBar is designed exclusively for development environments. Always wrap `app.UseNetDebugBar()` in a development environment check:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseNetDebugBar();
}
```

⚠️ **Performance** - The debug bar captures detailed information and should not be used in production environments as it may impact performance and expose sensitive data.

⚠️ **Security** - Debug information may contain sensitive data like connection strings, query parameters, and headers. Never enable in production.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

MIT License - see LICENSE file for details

## Acknowledgments

Inspired by [Laravel Debugbar](https://github.com/barryvdh/laravel-debugbar) and [MiniProfiler](https://miniprofiler.com/).
