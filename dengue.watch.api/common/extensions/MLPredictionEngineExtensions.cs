using dengue.watch.api.features.trainingdatapipeline.models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.ML;

namespace dengue.watch.api.common.extensions;
public static class MLPredictionEngineExtensions
{
    public static IServiceCollection AddMLPredictionEngine(this IServiceCollection services, IWebHostEnvironment env)
    {
        string filePath = Path.Combine(env.ContentRootPath, "infrastructure", "ml", "models", "DengueForecastModel.zip");
        services.AddPredictionEnginePool<LaggedDengueCausalData, DenguePrediction>()
        .FromFile(modelName: "DengueForecast", filePath: filePath, watchForChanges: true);
  
        return services;
    }
}
