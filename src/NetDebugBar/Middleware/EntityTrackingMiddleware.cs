using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NetDebugBar.Core;
using NetDebugBar.Core.Models;

namespace NetDebugBar.Middleware;

/// <summary>
/// Middleware that captures entity tracking information from registered DbContexts
/// after request execution completes.
/// </summary>
public class EntityTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly NetDebugBarOptions _options;

    public EntityTrackingMiddleware(RequestDelegate next, NetDebugBarOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context, NetDebugBarContext debug)
    {
        await _next(context);

        if (!_options.EnableModelsPanel || _options.TrackedDbContextTypes.Count == 0)
            return;

        try
        {
            var allEntities = new List<EntityModelInfo>();

            foreach (var dbContextType in _options.TrackedDbContextTypes)
            {
                var dbContext = context.RequestServices.GetService(dbContextType) as DbContext;
                if (dbContext == null) continue;

                var entities = dbContext.ChangeTracker.Entries()
                    .GroupBy(e => e.Metadata.ClrType.Name)
                    .Select(g => new EntityModelInfo
                    {
                        TypeName = g.Key,
                        Count = g.Count(),
                        AddedCount = g.Count(e => e.State == EntityState.Added),
                        ModifiedCount = g.Count(e => e.State == EntityState.Modified),
                        DeletedCount = g.Count(e => e.State == EntityState.Deleted),
                        UnchangedCount = g.Count(e => e.State == EntityState.Unchanged)
                    })
                    .OrderByDescending(e => e.Count)
                    .ToList();

                allEntities.AddRange(entities);
            }

            if (allEntities.Any())
            {
                if (debug.CurrentModels == null)
                    debug.CurrentModels = new ModelInfo();

                debug.CurrentModels.EntityModels = allEntities;
            }
        }
        catch
        {
            // Silently fail if DbContext unavailable
        }
    }
}
