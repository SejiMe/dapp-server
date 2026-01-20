using dengue.watch.api.features.weathersummary.services;

namespace dengue.watch.api.features.weathersummary;

public class WeatherSummaryFeature : IFeature
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IWeatherDataCoverageService, WeatherDataCoverageService>();
        return services;
    }
}