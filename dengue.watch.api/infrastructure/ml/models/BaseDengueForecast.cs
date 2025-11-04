using Microsoft.ML;

namespace dengue.watch.api.infrastructure.ml.models;

public class BaseDengueForecast
{
    protected readonly MLContext _mlContext;
    protected readonly ILogger _logger;
    protected readonly IWebHostEnvironment _hostEnv;
    protected double _standardDeviation;
    private const float OUTBREAK_THRESHOLD = 10f; // Adjust based on your region

    
    public BaseDengueForecast(
        MLContext mlContext,
        ILogger logger,
        IWebHostEnvironment hostEnv
        )
    {
        _mlContext = mlContext;
        _logger = logger;
        _hostEnv = hostEnv;
        _standardDeviation = 5.0; // Default
    }
    
    
    protected void LoadModelIfExists(string modelPath,string metricsPath, out ITransformer model)
    {
        model = null;
        if (File.Exists(modelPath))
        {
            try
            {
                model = _mlContext.Model.Load(modelPath, out _);
                _logger.LogInformation("Model loaded successfully from {Path}", modelPath);
                
                // Load metrics
                LoadMetricsIfExists(metricsPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model from {Path}", modelPath);
            }
        }
        else
        {
            _logger.LogInformation("No existing model found at {Path}", modelPath);
        }
    }

    protected void LoadMetricsIfExists(string metricsPath)
    {
        if (File.Exists(metricsPath))
        {
            try
            {
                var json = File.ReadAllText(metricsPath);
                var metrics = System.Text.Json.JsonSerializer.Deserialize<SavedMetrics>(json);
                if (metrics != null)
                {
                    _standardDeviation = metrics.StandardDeviation;
                    _logger.LogInformation("Loaded standard deviation: {StdDev:F2}", _standardDeviation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load metrics from {Path}", metricsPath);
                _standardDeviation = 5.0; // Default fallback
            }
        }
        else
        {
            _standardDeviation = 5.0; // Default fallback
        }
    }

    protected async Task SaveModelAsync(ITransformer? model, string modelPath)
    {
        if (model != null)
        {
            await Task.Run(() =>
            {
                _mlContext.Model.Save(model, null, modelPath);
                _logger.LogInformation("Model saved to {Path}", modelPath);
            });
        }
    }

    protected async Task SaveMetricsAsync(string metricsPath)
    {
        var metrics = new SavedMetrics
        {
            StandardDeviation = _standardDeviation,
            SavedAt = DateTime.UtcNow
        };

        var json = System.Text.Json.JsonSerializer.Serialize(metrics, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(metricsPath, json);
        _logger.LogInformation("Metrics saved to {Path}", metricsPath);
    }
    
    protected double CalculateStandardDeviation(IDataView predictions)
    {
        var results = _mlContext.Data.CreateEnumerable<PredictionResult>(predictions, false).ToList();
        
        if (results.Count == 0)
        {
            _logger.LogWarning("No test predictions available for standard deviation calculation");
            return 5.0; // Default fallback
        }
        
        // Calculate residuals (errors)
        var residuals = results.Select(r => r.DengueCount - r.Score).ToList();
        
        // Calculate standard deviation of errors
        double mean = residuals.Average();
        double variance = residuals.Sum(r => Math.Pow(r - mean, 2)) / residuals.Count;
        double stdDev = Math.Sqrt(variance);
        
        _logger.LogInformation(
            "Standard Deviation calculated from {Count} test predictions: {StdDev:F2}",
            results.Count,
            stdDev
        );
        
        return stdDev;
    }

    protected void EnrichPredictionWithStatistics(DengueForecastOutput prediction, double confidenceLevel)
    {
        // Calculate z-score for confidence level
        double zScore = confidenceLevel switch
        {
            0.95 => 1.96,
            0.90 => 1.645,
            0.80 => 1.28,
            0.99 => 2.576,
            _ => 1.96
        };

        double margin = zScore * _standardDeviation;

        // Set confidence interval
        prediction.LowerBound = Math.Max(0, prediction.Score - (float)margin);
        prediction.UpperBound = prediction.Score + (float)margin;
        prediction.ConfidencePercentage = confidenceLevel * 100;

        // Calculate outbreak probability
        prediction.ProbabilityOfOutbreak = CalculateOutbreakProbability(
            prediction.Score, 
            prediction.LowerBound, 
            prediction.UpperBound
        );
    }

    protected double CalculateOutbreakProbability(float predictedCases, float lowerBound, float upperBound)
    {
        // If even the lower bound exceeds threshold, very high probability
        if (lowerBound >= OUTBREAK_THRESHOLD)
        {
            return Math.Min(98, 80 + (predictedCases - OUTBREAK_THRESHOLD) * 1.5);
        }

        // If upper bound is below threshold, very low probability
        if (upperBound < OUTBREAK_THRESHOLD)
        {
            return Math.Max(2, (predictedCases / OUTBREAK_THRESHOLD) * 40);
        }

        // Partial overlap - calculate proportion above threshold
        float rangeAboveThreshold = upperBound - OUTBREAK_THRESHOLD;
        float totalRange = upperBound - lowerBound;
        
        if (totalRange == 0) return predictedCases >= OUTBREAK_THRESHOLD ? 90 : 10;
        
        double probability = (rangeAboveThreshold / totalRange) * 70 + 15;

        return Math.Min(95, Math.Max(5, probability));
    }
}