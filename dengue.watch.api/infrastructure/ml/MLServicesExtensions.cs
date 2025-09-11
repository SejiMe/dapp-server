namespace dengue.watch.api.infrastructure.ml;

/// <summary>
/// Extension methods for registering ML services
/// </summary>
public static class MLServicesExtensions
{
    /// <summary>
    /// Add ML.NET services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The configured service collection</returns>
    public static IServiceCollection AddMLServices(this IServiceCollection services)
    {
        // Register the dengue forecast service
        services.AddSingleton<IPredictionService<DengueForecastInput, DengueForecastOutput>, DengueForecastService>();
        
        // Register specific service for easier injection
        services.AddSingleton<DengueForecastService>();

        // Register weekly and monthly prediction services (stubs)
        services.AddSingleton<WeeklyDengueForecastService>();
        services.AddSingleton<MonthlyDengueForecastService>();

        return services;
    }
}
