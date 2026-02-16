# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NetDebugBar is a comprehensive debug toolbar for ASP.NET Core 9 applications inspired by Laravel Debug Bar. It displays detailed information about database queries, cache operations, logs, HTTP requests, and request timeline visualization. The library targets .NET 9.0 and is designed exclusively for development environments.

## Build and Run Commands

From the repository root (`F:\Workspace\c#\C#DebugBarTest\`):

```bash
# Build the entire solution
dotnet build

# Build only the NetDebugBar library
dotnet build src/NetDebugBar

# Run the demo application (to test changes)
dotnet run --project samples/NetDebugBar.Demo

# Clean build artifacts
dotnet clean
```

The working directory for this session is `src/NetDebugBar`, so adjust paths accordingly.

## Architecture

### Core Data Flow

1. **NetDebugBarContext** (scoped service) - Central request-scoped container holding all debug data for the current HTTP request. Contains lists for queries, cache operations, log entries, timeline events, and request info.

2. **NetDebugBarContextSnapshot** - Immutable snapshot of NetDebugBarContext state, used for POST-Redirect-GET pattern support.

3. **DebugBarStorage** (singleton) - Thread-safe in-memory storage that preserves debug data across redirects. Uses GUIDs as keys and automatically cleans up entries older than 60 seconds.

### Data Collection Mechanisms

The library uses four different techniques to capture debug data:

1. **EF Core Interceptor** (`NetDebugBarQueryInterceptor`) - Implements `DbCommandInterceptor` to capture SQL commands, parameters, and execution time. Must be manually registered when configuring DbContext.

2. **Decorator Pattern** (`NetDebugBarMemoryCache`) - Wraps `IMemoryCache` to intercept Get/Set/Remove operations. The decorator is automatically applied in `AddNetDebugBar()` by removing and replacing the existing `IMemoryCache` registration.

3. **Logger Provider** (`DebugBarLoggerProvider`) - Implements `ILoggerProvider` to capture log entries. Each logger instance writes to the current request's NetDebugBarContext via `IHttpContextAccessor`.

4. **DiagnosticListener** (`TimelineDiagnosticObserver`) - Subscribes to `Microsoft.AspNetCore` diagnostic events to track request pipeline phases (routing, authorization, handler execution, etc.).

### Middleware Pipeline Order

The middleware must be registered in this specific order (enforced in `ApplicationBuilderExtensions.UseNetDebugBar()`):

1. **TimelineMiddleware** - Outermost middleware that wraps the entire request to capture full lifecycle timing.

2. **DebugBarMiddleware** - Manages POST-Redirect-GET pattern:
   - On GET: Checks for `__NetDebugBarPrev` cookie, retrieves snapshot from storage, deletes cookie
   - On POST redirect: Creates snapshot, stores it, sets cookie with storage ID

3. **HtmlInjectionMiddleware** - Innermost middleware that:
   - Buffers the response using a `MemoryStream`
   - Checks if response is HTML (`text/html` content type)
   - Generates debug bar HTML via `DebugBarHtmlRenderer`
   - Injects before `</body>` tag (or appends at end if no closing tag)

### Panel System

All panels implement `IDebugBarPanel`:
- `TabId` - Unique identifier for the panel
- `TabLabel` - Display name in tab bar
- `RenderTabContent()` - Generates panel HTML
- `RenderBadge()` - Optional badge text (e.g., query count)

Panels are instantiated in `DebugBarHtmlRenderer` constructor based on `NetDebugBarOptions`. The Overview panel is always included. All HTML, CSS, and JavaScript are inlined (no external files).

## Configuration

The `NetDebugBarOptions` class controls:
- Panel enable/disable flags (all default to true)
- Query performance thresholds: `SlowQueryThresholdMs` (150ms), `MediumQueryThresholdMs` (50ms)
- `MinimumLogLevel` for log capture (default: Debug)
- `AccentColor` for UI theming (default: #a855f7 purple)

## Key Implementation Patterns

### Decorator Registration

The `DecorateMemoryCache()` method in `ServiceCollectionExtensions` demonstrates a complex service replacement pattern:
1. Finds existing `IMemoryCache` descriptor
2. Removes it from service collection
3. Re-registers with factory that recreates original instance (handling `ImplementationFactory`, `ImplementationInstance`, or `ImplementationType`)
4. Wraps original in `NetDebugBarMemoryCache` decorator

### POST-Redirect-GET Support

Uses a cookie-based storage approach to avoid cookie size limits:
1. POST request is processed, debug data accumulates in `NetDebugBarContext`
2. On redirect response, middleware creates `NetDebugBarContextSnapshot` and stores in `DebugBarStorage`
3. Sets small cookie containing only the storage ID (GUID)
4. Redirected GET request reads cookie, retrieves snapshot from storage
5. Snapshot is attached as `PreviousSnapshot` on new `NetDebugBarContext`
6. Panels render both "Previous Request" and "Current Request" sections

### HTML Injection Without Layout Modification

The `HtmlInjectionMiddleware` uses response buffering to avoid requiring changes to Razor layout files. It replaces `HttpContext.Response.Body` with a `MemoryStream`, lets the pipeline execute, then reads the buffered HTML, injects the debug bar, and writes the modified content to the original response stream.

## Important Notes

- Always wrap `app.UseNetDebugBar()` in `if (app.Environment.IsDevelopment())` checks
- The EF Core interceptor must be manually added to DbContext options: `options.UseSqlServer(...).AddInterceptors(interceptor)` where interceptor is `NetDebugBarQueryInterceptor`
- When adding new panels, implement `IDebugBarPanel` and register in `DebugBarHtmlRenderer` constructor
- All rendering is server-side; no AJAX or separate endpoints
- Timeline events are captured via `DiagnosticListener.AllListeners.Subscribe(observer)` which must happen in `UseNetDebugBar()`, not in service registration

## Testing Strategy

### Unit Tests

**Core Components:**
- **NetDebugBarContext** - Test data accumulation (queries, cache ops, logs, timeline events)
- **DebugBarStorage** - Test Store/Retrieve operations, GUID generation, 60-second cleanup logic
- **NetDebugBarContextSnapshot** - Test snapshot creation preserves all data correctly

**Data Collectors:**
- **NetDebugBarQueryInterceptor**
  - SQL command capture with parameters
  - Timing accuracy
  - Handling of batched commands
  - Different parameter types (string, int, DateTime, null)

- **NetDebugBarMemoryCache**
  - Decorator properly forwards all IMemoryCache operations
  - GET operations tracked as HIT/MISS
  - SET and REMOVE operations captured
  - Works when no NetDebugBarContext exists (non-request scenarios)

- **DebugBarLoggerProvider/DebugBarLogger**
  - Log filtering by MinimumLogLevel
  - Exception capture with stack traces
  - Category names preserved
  - Handles concurrent logging

- **TimelineDiagnosticObserver**
  - Subscribes to correct diagnostic events
  - Captures timing information accurately
  - Handles missing/null HttpContext gracefully

**Rendering:**
- **Individual Panels** (QueriesPanel, CachePanel, etc.)
  - `RenderBadge()` returns correct counts
  - `RenderTabContent()` produces valid HTML
  - Handles empty data gracefully
  - Color-coding thresholds work correctly (slow/medium/fast queries)

- **DebugBarHtmlRenderer**
  - Includes only enabled panels based on options
  - Overview panel always included
  - Generated HTML is well-formed
  - CSS and JS are included

### Integration Tests

**Service Registration:**
- `AddNetDebugBar()` registers all required services
- IMemoryCache decorator replacement works correctly
- Conditional panel services based on options
- Doesn't break when IMemoryCache doesn't pre-exist

**Middleware Pipeline:**
- **DebugBarMiddleware**
  - POST-Redirect-GET: cookie set on 3xx redirect after POST
  - Snapshot retrieval and cookie deletion on subsequent GET
  - Doesn't interfere with non-redirect POSTs
  - Handles missing/corrupted storage IDs gracefully

- **HtmlInjectionMiddleware**
  - Injects before `</body>` tag when present
  - Appends at end when no `</body>` found
  - Skips injection for non-HTML responses (JSON, XML, images)
  - Preserves response when errors occur
  - Updates Content-Length header correctly

- **TimelineMiddleware**
  - Captures full request duration
  - Works with DiagnosticObserver

**Middleware Ordering:**
- Test that `UseNetDebugBar()` registers middleware in correct order
- Timeline wraps everything, injection is innermost

### Functional/End-to-End Tests

Use `WebApplicationFactory<T>` from Microsoft.AspNetCore.Mvc.Testing:

**Full Request Scenarios:**
- HTML page request shows debug bar in response
- Database query via EF Core appears in Queries panel
- Cache operations appear in Cache panel
- Log messages appear in Logging panel
- Timeline shows request phases

**POST-Redirect-GET Pattern:**
- Submit form (POST) → redirect → following GET shows both requests
- Previous request data preserved across redirect
- Storage cleanup after 60 seconds

**Edge Cases:**
- Very large responses (ensure buffering doesn't cause issues)
- Concurrent requests don't mix debug data
- Missing HttpContext handled gracefully
- Disabled panels don't collect data or render

### Implementation Priority

If writing tests incrementally:

1. **DebugBarStorage** - Core PRG functionality
2. **DebugMemoryCache decorator** - Complex DI pattern
3. **HtmlInjectionMiddleware** - Most critical for user experience
4. **NetDebugBarQueryInterceptor** - Most complex data collector
5. **End-to-end PRG test** - Validates the whole system

The middleware and decorator patterns are the trickiest parts of this codebase, so those should definitely have test coverage.
