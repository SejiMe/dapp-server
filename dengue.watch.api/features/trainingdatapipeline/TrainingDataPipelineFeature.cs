
using dengue.watch.api.features.trainingdatapipeline.services;

namespace dengue.watch.api.features.trainingdatapipeline;


public class TrainingDataPipeline : IFeature {
    public static IServiceCollection ConfigureServices(IServiceCollection services) {
        services.AddScoped<IWeekExtractorService, WeekExtractorService>();
        services.AddScoped<IYearExtractorService, YearExtractorService>();
        services.AddScoped<ITrainingDataCsvService, TrainingDataCsvService>();
        return services;
    }
}