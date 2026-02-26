# NetDebugBar

![A simple debug bar at the bottom of a webpage](https://i.ibb.co/PZtj8FH7/Screenshot-2026-02-25-235241.png)

A comprehensive debug toolbar for ASP.NET Core 9 applications, inspired by Laravel Debug Bar. NetDebugBar provides real-time insights into database queries, cache operations, logs, HTTP requests, and request timeline visualization.

![.NET Version](https://img.shields.io/badge/.NET-9.0-purple)
![License](https://img.shields.io/badge/license-MIT-blue)

## Repository Structure

This repository contains:
- **src/NetDebugBar/** - The reusable NetDebugBar class library
- **samples/NetDebugBar.Demo/** - Demo Razor Pages application showcasing all features

## Features

- **Database Query Tracking** - Captures all Entity Framework Core queries with execution time, parameters, and performance color-coding
- **N+1 Query Detection** - Automatically identifies potential N+1 query problems by detecting similar queries executed in rapid succession
- **Cache Operations** - Monitors IMemoryCache operations (Get/Set/Remove) with hit/miss tracking and statistics
- **Logging Panel** - Displays all log entries with filtering by log level (Debug, Info, Warning, Error, Critical)
- **Request Information** - Shows HTTP method, path, status code, headers, cookies, and request duration
- **Timeline Visualization** - Visual timeline of request pipeline phases (routing, authorization, middleware, etc.)
- **POST-Redirect-GET Support** - Preserves debug data across redirects to show both previous and current request information
- **Zero Configuration** - No layout modifications required; automatically injects into HTML responses

## Installation

### Option 1: NuGet Package (Coming Soon)

```bash
dotnet add package NetDebugBar
```

### Option 2: Clone and Build Locally

```bash
git clone <repository-url>
cd NetDebugBar
dotnet build
# Reference the project or use the built DLL
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

    // N+1 query detection
    options.EnableNPlusOneDetection = true;  // Enable/disable N+1 detection
    options.NPlusOneThreshold = 3;           // Minimum similar queries to flag as N+1

    // Logging configuration
    options.MinimumLogLevel = LogLevel.Debug;

    // UI customization
    options.AccentColor = "#a855f7";  // Purple accent color (default)
    // Supports any CSS color: hex, rgb, rgba, hsl, or named colors
    // Examples: "#ff0000", "rgb(24, 120, 184)", "rgba(255, 0, 0, 0.8)", "blue"
});
```

### UI Customization

The `AccentColor` option allows you to customize the color theme of the debug bar. This single setting affects all accent elements throughout the UI:

- **Header top border** - 5px colored stripe at the top of the debug bar
- **Active tab** - Background color of the currently selected panel tab
- **Dashboard cards** - Accent borders and value colors in the Overview panel
- **Interactive elements** - Hover states for expandable sections
- **Timeline** - Total request duration display

**Supported Color Formats:**
- Hex: `#a855f7`, `#f00`
- RGB: `rgb(24, 120, 184)`
- RGBA: `rgba(168, 85, 247, 0.8)`
- HSL: `hsl(271, 91%, 65%)`
- Named: `purple`, `blue`, `red`

**Default:** `#a855f7` (purple)

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
- **N+1 query detection** - Automatically flags groups of similar queries that indicate potential N+1 problems
- Warning indicators when N+1 patterns are detected
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

### Data Collection

NetDebugBar uses several techniques to capture debug data:

1. **EF Core Interceptor** (`NetDebugBarQueryInterceptor` implements `DbCommandInterceptor`) - Captures SQL queries, parameters, and execution time
2. **Decorator Pattern** (`NetDebugBarMemoryCache` wraps `IMemoryCache`) - Intercepts cache Get/Set/Remove operations
3. **Logger Provider** (`DebugBarLoggerProvider` implements `ILoggerProvider`) - Captures all log entries written via `ILogger`
4. **Diagnostic Listener** (`TimelineDiagnosticObserver`) - Subscribes to ASP.NET Core diagnostic events for request pipeline phases
5. **Response Buffering Middleware** - Injects HTML into responses before `</body>` tag without requiring layout modifications

### Middleware Pipeline

The debug bar uses three middleware components registered in this order:

1. **TimelineMiddleware** - Outermost middleware capturing full request timing
2. **DebugBarMiddleware** - Handles POST-Redirect-GET pattern with server-side storage
3. **HtmlInjectionMiddleware** - Buffers response and injects debug bar HTML

### POST-Redirect-GET Support

To preserve debug data across redirects without hitting cookie size limits:
- POST request data is captured in `NetDebugBarContext`
- On redirect response (3xx), a snapshot is stored server-side with a GUID key
- Small cookie contains only the GUID (not the full debug data)
- Redirected GET request retrieves the snapshot and displays both "Previous Request" and "Current Request"
- Snapshots auto-cleanup after 60 seconds

The middleware automatically:
- Detects HTML responses (`text/html` content type)
- Generates debug bar HTML with inline CSS/JavaScript (no external files)
- Injects before closing `</body>` tag (or appends if no closing tag found)
- Handles concurrent requests safely with request-scoped data collection

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

First-time setup:

```bash
# Copy the example appsettings file
cp samples/NetDebugBar.Demo/appsettings.json.example samples/NetDebugBar.Demo/appsettings.json
```

Then run the application:

```bash
dotnet run --project samples/NetDebugBar.Demo
```

Navigate to `http://localhost:5284` (or the URL shown in console output) to see NetDebugBar in action with a full CRUD interface demonstrating all panels.

### Project Structure

```
NetDebugBar/
├── src/NetDebugBar/                    # Main library (.NET 9.0)
│   ├── Core/                           # NetDebugBarContext, DebugBarStorage, models
│   ├── Extensions/                     # DI registration (AddNetDebugBar, UseNetDebugBar)
│   ├── Middleware/                     # DebugBarMiddleware, HtmlInjectionMiddleware, TimelineMiddleware
│   ├── Rendering/                      # DebugBarHtmlRenderer
│   │   └── Panels/                     # IDebugBarPanel implementations (6 panels)
│   ├── Logging/                        # DebugBarLoggerProvider, DebugBarLogger
│   ├── Interceptors/                   # NetDebugBarQueryInterceptor (EF Core)
│   ├── Cache/                          # NetDebugBarMemoryCache (IMemoryCache decorator)
│   ├── Timeline/                       # TimelineDiagnosticObserver
│   ├── Utils/                          # Helper methods
│   ├── CLAUDE.md                       # Library-specific development guide
│   └── README.md                       # Library documentation
├── samples/NetDebugBar.Demo/           # Demo Razor Pages application
│   ├── Data/                           # DbContext and entity models
│   ├── Pages/                          # Razor Pages (Games CRUD)
│   └── Program.cs                      # App configuration with NetDebugBar
├── CLAUDE.md                           # Repository development guide
├── README.md                           # This file
└── NetDebugBar.sln                     # Solution file
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
