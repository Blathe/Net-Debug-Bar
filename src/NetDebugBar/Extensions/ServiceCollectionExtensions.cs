using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using NetDebugBar.Core;
using NetDebugBar.Interceptors;
using NetDebugBar.Cache;
using NetDebugBar.Logging;
using NetDebugBar.Timeline;

namespace NetDebugBar.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetDebugBar(
        this IServiceCollection services,
        Action<NetDebugBarOptions>? configure = null)
    {
        var options = new NetDebugBarOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        // Core services
        services.AddHttpContextAccessor();
        services.AddScoped<NetDebugBarContext>();
        services.AddSingleton<DebugBarStorage>();

        // EF Core query interceptor
        if (options.EnableQueryPanel)
            services.AddScoped<NetDebugBarQueryInterceptor>();

        // IMemoryCache decorator
        if (options.EnableCachePanel)
        {
            DecorateMemoryCache(services);
        }

        // Logging provider
        if (options.EnableLoggingPanel)
        {
            services.AddSingleton<ILoggerProvider, DebugBarLoggerProvider>();
        }

        // Timeline diagnostic observer
        if (options.EnableTimelinePanel)
        {
            services.AddSingleton<TimelineDiagnosticObserver>();
        }

        return services;
    }

    private static void DecorateMemoryCache(IServiceCollection services)
    {
        // Find existing IMemoryCache registration
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMemoryCache));

        if (existingDescriptor != null)
        {
            // Remove existing registration
            services.Remove(existingDescriptor);

            // Re-register with decorator
            services.AddSingleton<IMemoryCache>(sp =>
            {
                // Reconstruct the original IMemoryCache
                IMemoryCache innerCache;
                if (existingDescriptor.ImplementationFactory != null)
                {
                    innerCache = (IMemoryCache)existingDescriptor.ImplementationFactory(sp);
                }
                else if (existingDescriptor.ImplementationInstance != null)
                {
                    innerCache = (IMemoryCache)existingDescriptor.ImplementationInstance;
                }
                else if (existingDescriptor.ImplementationType != null)
                {
                    innerCache = (IMemoryCache)ActivatorUtilities.CreateInstance(sp, existingDescriptor.ImplementationType);
                }
                else
                {
                    innerCache = new MemoryCache(new MemoryCacheOptions());
                }

                return new NetDebugBarMemoryCache(innerCache, sp.GetRequiredService<IHttpContextAccessor>());
            });
        }
        else
        {
            // No IMemoryCache registered yet, add one and decorate it
            services.AddSingleton<IMemoryCache>(sp =>
            {
                var innerCache = new MemoryCache(new MemoryCacheOptions());
                return new NetDebugBarMemoryCache(innerCache, sp.GetRequiredService<IHttpContextAccessor>());
            });
        }
    }
}
