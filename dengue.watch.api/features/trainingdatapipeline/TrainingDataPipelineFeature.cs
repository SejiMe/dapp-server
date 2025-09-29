using dengue.watch.api.features.trainingdatapipeline.repositories;
using dengue.watch.api.features.trainingdatapipeline.services;

namespace dengue.watch.api.features.trainingdatapipeline;


public class TrainingDataPipeline : IFeature {
    public static IServiceCollection ConfigureServices(IServiceCollection services) {
        services.AddScoped<IWeekExtractorService, WeekExtractorService>();
        services.AddScoped<IYearExtractorService, YearExtractorService>();
        services.AddScoped<ITemperatureStatisticsService, TemperatureStatisticsService>();
        services.AddScoped<IHumidityStatisticsService, HumidityStatisticsService>();
        services.AddScoped<IPrecipitationStatisticsService, PrecipitationStatisticsService>();
        services.AddScoped<IWeatherCodeStatisticsService, WeatherCodeStatisticsService>();
        services.AddScoped<ITrainingDataWeeklyStatisticsService, TrainingDataWeeklyStatisticsService>();
        services.AddScoped<ITrainingDataCsvService, TrainingDataCsvService>();
        services.AddScoped<IWeeklyTrainingWeatherRepository, WeeklyTrainingWeatherRepository>();

        return services;
    }
}