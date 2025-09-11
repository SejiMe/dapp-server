using System.Reflection;
using dengue.watch.api.common.interfaces;

namespace dengue.watch.api.common.extensions;

/// <summary>
/// Extensions for discovering and registering services through reflection
/// </summary>
public static class ServiceDiscoveryExtensions
{
    /// <summary>
    /// Discover and register all features in the assembly
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">The assembly to scan (defaults to calling assembly)</param>
    /// <returns>The configured service collection</returns>
    public static IServiceCollection DiscoverFeatures(this IServiceCollection services, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        var featureTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IFeature)))
            .ToList();

        foreach (var featureType in featureTypes)
        {
            try
            {
                var configureMethod = featureType.GetMethod("ConfigureServices", 
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(IServiceCollection)],
                    null);

                if (configureMethod != null)
                {
                    configureMethod.Invoke(null, [services]);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure feature {featureType.Name}: {ex.Message}", ex);
            }
        }

        return services;
    }

    /// <summary>
    /// Discover and map all endpoints in the assembly
    /// </summary>
    /// <param name="app">The endpoint route builder</param>
    /// <param name="assembly">The assembly to scan (defaults to calling assembly)</param>
    /// <returns>The configured endpoint route builder</returns>
    public static IEndpointRouteBuilder DiscoverEndpoints(this IEndpointRouteBuilder app, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        var endpointTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IEndpoint)))
            .ToList();

        foreach (var endpointType in endpointTypes)
        {
            try
            {
                var mapMethod = endpointType.GetMethod("MapEndpoints",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(IEndpointRouteBuilder)],
                    null);

                if (mapMethod != null)
                {
                    mapMethod.Invoke(null, [app]);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to map endpoints for {endpointType.Name}: {ex.Message}", ex);
            }
        }

        return app;
    }

    /// <summary>
    /// Discover and map all SignalR hubs in the assembly
    /// </summary>
    /// <param name="app">The endpoint route builder</param>
    /// <param name="assembly">The assembly to scan (defaults to calling assembly)</param>
    /// <returns>The configured endpoint route builder</returns>
    public static IEndpointRouteBuilder DiscoverHubs(this IEndpointRouteBuilder app, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        var hubTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IHub)))
            .ToList();

        foreach (var hubType in hubTypes)
        {
            try
            {
                var mapMethod = hubType.GetMethod("MapHub",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(IEndpointRouteBuilder)],
                    null);

                if (mapMethod != null)
                {
                    mapMethod.Invoke(null, [app]);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to map hub {hubType.Name}: {ex.Message}", ex);
            }
        }

        return app;
    }
}
