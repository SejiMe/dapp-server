namespace dengue.watch.api.common.extensions;
public static class CommonRepositoryExtensions
{
    public static IServiceCollection AddCommonRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAggregatedWeeklyHistoricalWeatherRepository, AggregatedWeeklyHistoricalWeatherRepository>();
        return services;
    }
}