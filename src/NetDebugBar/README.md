# NetDebugBar

A comprehensive debug bar for ASP.NET Core 9 applications, inspired by Laravel Debug Bar. Displays detailed information about queries, cache operations, logs, request details, and timeline visualization.

## Features

- **📊 Overview Dashboard** - Summary cards showing total queries, duration, cache operations, and logs
- **🔍 Queries Panel** - SQL queries with parameters, execution time, and color-coded performance indicators
- **🌐 Request Panel** - HTTP method, path, status, headers, cookies, and query parameters
- **💾 Cache Panel** - Memory cache operations with hit/miss tracking and statistics
- **📝 Logging Panel** - Captured log messages with filtering by level
- **⏱️ Timeline Panel** - Visual timeline of request lifecycle events
- **🔄 POST-Redirect-GET Support** - Preserves debug data across redirects (e.g. viewing form submission information after being redirected.)

## Installation

### 1. Add the Library Reference

Add a project reference to your ASP.NET Core application:

```xml
<ItemGroup>
  <ProjectReference Include="path\to\NetDebugBar\NetDebugBar.csproj" />
</ItemGroup>
```

### 2. Register Services

In your `Program.cs`, add NetDebugBar services:

```csharp
using NetDebugBar.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add NetDebugBar (development only)
builder.Services.AddNetDebugBar(options =>
{
    options.SlowQueryThresholdMs = 150;      // Queries slower than this are marked red
    options.MediumQueryThresholdMs = 50;     // Queries slower than this are marked yellow
    options.MinimumLogLevel = LogLevel.Debug; // Minimum log level to capture
    options.AccentColor = "#a855f7";         // Purple accent color
});

// Other services...
```

### 3. Configure EF Core Interceptor (Optional but Recommended)

To capture database queries, register the interceptor with your DbContext:

```csharp
using NetDebugBar.Interceptors;

builder.Services.AddDbContext<YourDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<NetDebugBarQueryInterceptor>();
    options.UseSqlServer(connectionString).AddInterceptors(interceptor);
});
```

### 4. Use Middleware

Add NetDebugBar middleware in the HTTP pipeline (development only):

```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseNetDebugBar(); // Add this line
}

// Other middleware...
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
```

## Usage

Once installed, the debug bar will automatically appear at the bottom of all HTML pages in development mode.

### No Layout Modification Required

Unlike other debug tools, NetDebugBar automatically injects itself into your HTML responses via middleware - no need to modify your `_Layout.cshtml` file!

### Panels

Click the tabs to switch between different panels:

1. **Overview** - Dashboard with summary metrics
2. **Request** - HTTP request details with status code badge
3. **Queries** - Database queries with parameter values
4. **Cache** - Cache operations with hit rate statistics
5. **Logs** - Application logs with level filtering
6. **Timeline** - Visual timeline of request phases

### Collapse/Expand

Click the `−` button in the top-right corner to collapse the debug bar to a minimal size.

## Configuration Options

```csharp
builder.Services.AddNetDebugBar(options =>
{
    // Enable/disable individual panels
    options.EnableQueryPanel = true;     // Default: true
    options.EnableRequestPanel = true;   // Default: true
    options.EnableCachePanel = true;     // Default: true
    options.EnableTimelinePanel = true;  // Default: true
    options.EnableLoggingPanel = true;   // Default: true

    // Query performance thresholds
    options.SlowQueryThresholdMs = 150;      // Red indicator
    options.MediumQueryThresholdMs = 50;     // Yellow indicator

    // Logging
    options.MinimumLogLevel = LogLevel.Debug; // Minimum level to capture

    // Appearance
    options.AccentColor = "#a855f7"; // Customize the accent color
});
```

## POST-Redirect-GET Pattern

NetDebugBar automatically preserves debug data across POST-Redirect-GET patterns. When you submit a form and get redirected, you'll see both:
- **Previous Request** section showing the POST request data
- **Current Request** section showing the GET request after redirect

This works for all panels (queries, cache, logs, timeline).

## Query Panel Features

- **SQL Syntax** - Full SQL statements displayed
- **Parameters** - Expandable parameter section showing names, types, and values
- **Performance** - Color-coded durations:
  - 🟢 Green: Fast (≤ 50ms by default)
  - 🟡 Yellow: Medium (≤ 150ms by default)
  - 🔴 Red: Slow (> 150ms by default)
- **Batching** - Automatically splits batched SQL commands

## Cache Panel Features

- **Hit Rate** - Color-coded percentage (green > 75%, amber 50-75%, red < 50%)
- **Operations** - GET (with hit/miss), SET, REMOVE
- **Performance** - Duration for each operation
- **Value Types** - Shows the .NET type of cached values

## Logging Panel Features

- **Filtering** - Click level badges to show/hide logs
- **Levels** - Trace, Debug, Information, Warning, Error, Critical
- **Exception Details** - Expandable exception stack traces
- **Categories** - Full logger category names displayed

## Timeline Panel Features

- **Visual Bars** - Horizontal bars show timing and duration
- **Events Captured**:
  - Request Pipeline (full duration)
  - HTTP Request Start
  - Endpoint Matched (routing)
  - Authorization
  - Handler Execution (page handler/action method)
  - Result Execution (view rendering)
- **Hover** - Tooltip shows event name and duration

## Architecture

NetDebugBar uses:
- **Middleware** - Captures request/response data and injects HTML
- **EF Core Interceptor** - Captures database commands
- **IMemoryCache Decorator** - Tracks cache operations
- **ILoggerProvider** - Captures log messages
- **DiagnosticListener** - Observes ASP.NET Core pipeline events
- **Server-side Storage** - Preserves data across redirects (avoids cookie size limits)

## Development Only

NetDebugBar is designed for development environments only. Always wrap registration in an environment check:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseNetDebugBar();
}
```

## Requirements

- .NET 9.0
- ASP.NET Core 9.0
- Entity Framework Core 9.0 (optional, for query tracking)

## License

[Specify your license here]
