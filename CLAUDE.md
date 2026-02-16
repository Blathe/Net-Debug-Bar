# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NetDebugBar is a comprehensive debug toolbar for ASP.NET Core 9 applications inspired by Laravel Debug Bar. This repository contains:
- **src/NetDebugBar/** - Reusable class library (.NET 9.0)
- **samples/NetDebugBar.Demo/** - Demo Razor Pages application

The library displays detailed information about database queries, cache operations, logs, HTTP requests, and request timeline visualization, designed exclusively for development environments.

## Build and Run Commands

From repository root:

```bash
# Build the entire solution
dotnet build

# Build only the library
dotnet build src/NetDebugBar

# Build only the demo app
dotnet build samples/NetDebugBar.Demo

# Run the demo application
dotnet run --project samples/NetDebugBar.Demo

# Clean all build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

The demo app runs on http://localhost:5284 (or http://localhost:5000) and demonstrates all NetDebugBar features with a Games CRUD interface.

## Architecture Overview

### Solution Structure

```
NetDebugBar.sln
├── src/NetDebugBar/              # Reusable library
│   ├── Core/                     # NetDebugBarContext, DebugBarStorage, models
│   ├── Extensions/               # DI registration (AddNetDebugBar, UseNetDebugBar)
│   ├── Middleware/               # DebugBarMiddleware, HtmlInjectionMiddleware, TimelineMiddleware
│   ├── Interceptors/             # NetDebugBarQueryInterceptor (EF Core)
│   ├── Cache/                    # NetDebugBarMemoryCache (IMemoryCache decorator)
│   ├── Logging/                  # DebugBarLoggerProvider, DebugBarLogger
│   ├── Timeline/                 # TimelineDiagnosticObserver (DiagnosticListener)
│   ├── Rendering/                # DebugBarHtmlRenderer
│   │   └── Panels/              # IDebugBarPanel implementations (6 panels)
│   └── Utils/                    # Helper methods
└── samples/NetDebugBar.Demo/     # Demo ASP.NET Core app
```

### Core Data Flow

**Request-Scoped Container:**
- **NetDebugBarContext** (scoped) - Central container holding all debug data for current request (queries, cache ops, logs, timeline events, request info)
- **NetDebugBarContextSnapshot** - Immutable snapshot for POST-Redirect-GET pattern
- **DebugBarStorage** (singleton) - Thread-safe in-memory storage preserving data across redirects (60-second auto-cleanup)

**Data Collection Mechanisms:**
1. **EF Core Interceptor** (`NetDebugBarQueryInterceptor`) - Implements `DbCommandInterceptor` to capture SQL queries, parameters, and timing
2. **Decorator Pattern** (`NetDebugBarMemoryCache`) - Wraps `IMemoryCache` to intercept Get/Set/Remove operations
3. **Logger Provider** (`DebugBarLoggerProvider`) - Implements `ILoggerProvider` to capture log entries via `IHttpContextAccessor`
4. **DiagnosticListener** (`TimelineDiagnosticObserver`) - Subscribes to ASP.NET Core diagnostic events for request pipeline phases

### Middleware Pipeline Order

The middleware is registered in strict order by `UseNetDebugBar()`:

```
TimelineMiddleware              # Outermost - captures full request timing
  ↓
DebugBarMiddleware             # POST-Redirect-GET cookie handling
  ↓
HtmlInjectionMiddleware        # Response buffering + HTML injection before </body>
  ↓
[Application Middleware]
```

**HtmlInjectionMiddleware** uses response buffering to inject debug bar HTML without requiring layout file modifications. It replaces `HttpContext.Response.Body` with `MemoryStream`, lets the pipeline execute, then injects the debug bar HTML before `</body>`.

### POST-Redirect-GET Pattern

To avoid cookie size limits, uses server-side storage:
1. POST request accumulates debug data in `NetDebugBarContext`
2. On redirect (3xx) response, middleware creates `NetDebugBarContextSnapshot` and stores in `DebugBarStorage` with GUID key
3. Sets small cookie containing only the GUID
4. Redirected GET retrieves snapshot from storage using cookie GUID
5. Snapshot attached as `PreviousSnapshot` on new context
6. All panels render both "Previous Request" and "Current Request" sections

### IMemoryCache Decorator Registration

The `DecorateMemoryCache()` method demonstrates a complex service replacement pattern:
1. Finds existing `IMemoryCache` descriptor in service collection
2. Removes original registration
3. Re-registers with factory that recreates original instance (handling `ImplementationFactory`, `ImplementationInstance`, or `ImplementationType`)
4. Wraps original in `NetDebugBarMemoryCache` decorator

This avoids dependency on Scrutor and handles all registration patterns.

## Consumer API

**Minimal integration:**
```csharp
// Program.cs
builder.Services.AddNetDebugBar();

if (app.Environment.IsDevelopment())
{
    app.UseNetDebugBar();
}
```

**With EF Core query tracking:**
```csharp
builder.Services.AddNetDebugBar();

builder.Services.AddDbContext<MyDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<NetDebugBarQueryInterceptor>();
    options.UseSqlServer(connectionString).AddInterceptors(interceptor);
});

if (app.Environment.IsDevelopment())
{
    app.UseNetDebugBar();
}
```

**Configuration:**
```csharp
builder.Services.AddNetDebugBar(options =>
{
    options.EnableQueryPanel = true;
    options.EnableCachePanel = true;
    options.EnableLoggingPanel = true;
    options.EnableRequestPanel = true;
    options.EnableTimelinePanel = true;
    options.SlowQueryThresholdMs = 150;      // Red
    options.MediumQueryThresholdMs = 50;     // Yellow
    options.MinimumLogLevel = LogLevel.Debug;
    options.AccentColor = "#a855f7";         // Purple
});
```

## Panel System

All panels implement `IDebugBarPanel`:
- `TabId` - Unique identifier
- `TabLabel` - Display name in tab bar
- `RenderTabContent()` - Generates panel HTML
- `RenderBadge()` - Optional badge (e.g., query count)

**Available Panels:**
1. **OverviewPanel** - Always included, aggregate statistics
2. **QueriesPanel** - EF Core queries with timing and parameters
3. **CachePanel** - IMemoryCache operations with hit/miss tracking
4. **LoggingPanel** - Application logs filtered by level
5. **RequestPanel** - HTTP request/response details
6. **TimelinePanel** - Request pipeline visualization

Panels are instantiated in `DebugBarHtmlRenderer` based on `NetDebugBarOptions`. All CSS/JavaScript are embedded as C# strings (no external files or Razor dependency).

## Important Implementation Notes

- **Development Only** - Always wrap `app.UseNetDebugBar()` in `if (app.Environment.IsDevelopment())` checks
- **EF Core Interceptor** - Must be manually added to DbContext: `options.UseSqlServer(...).AddInterceptors(interceptor)`
- **No Layout Changes** - HTML injection happens automatically via middleware
- **Server-Side Rendering** - No AJAX or separate endpoints; all HTML generated server-side
- **Timeline Setup** - `DiagnosticListener.AllListeners.Subscribe(observer)` must happen in `UseNetDebugBar()`, not in service registration
- **CSS Class Prefix** - All CSS classes use `ndb-` prefix to avoid collisions with application styles

## Adding New Panels

To add a new panel:
1. Create class implementing `IDebugBarPanel` in `src/NetDebugBar/Rendering/Panels/`
2. Implement required methods: `TabId`, `TabLabel`, `RenderTabContent()`, `RenderBadge()`
3. Add enable/disable option to `NetDebugBarOptions`
4. Register panel in `DebugBarHtmlRenderer` constructor based on option
5. Add data collection mechanism if needed (interceptor, decorator, listener, etc.)
