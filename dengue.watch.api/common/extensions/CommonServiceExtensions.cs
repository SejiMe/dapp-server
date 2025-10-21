namespace dengue.watch.api.common.extensions;

public static class CommonServiceExtensions
{
    public static IServiceCollection AddStatisticsServiceExtensions(this IServiceCollection services)
    {
        services.AddScoped<ITemperatureStatisticsService, TemperatureStatisticsService>();
        services.AddScoped<IHumidityStatisticsService, HumidityStatisticsService>();
        services.AddScoped<IPrecipitationStatisticsService, PrecipitationStatisticsService>();
        services.AddScoped<IWeatherCodeStatisticsService, WeatherCodeStatisticsService>();
        services.AddScoped<IWeeklyDataStatisticsService, WeeklyDataStatisticsService>();
        return services;
    }
}
