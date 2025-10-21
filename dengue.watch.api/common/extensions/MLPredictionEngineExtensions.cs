using Microsoft.Extensions.ML;

namespace dengue.watch.api.common.extensions;
public static class MLPredictionEngineExtensions
{
    public static IServiceCollection AddMLPredictionEngine(this IServiceCollection services, IWebHostEnvironment env)
    {
        string filePath = Path.Combine(env.ContentRootPath, "infrastructure", "ml", "models", "DengueForecastModel.zip");
        
        // if (!File.Exists(filePath))
        // {
        //     throw new FileNotFoundException($"ML model file not found at: {filePath}");
        // }
        
        services.AddPredictionEnginePool<DengueForecastInput, DengueForecastOutput>()
        .FromFile(modelName: "DengueForecast", filePath: filePath, watchForChanges: true);
  
        return services;
    }
}
