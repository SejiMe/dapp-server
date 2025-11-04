// using System.Globalization;
// using CsvHelper;
// using CsvHelper.Configuration;
// using Microsoft.ML;
// using Microsoft.ML.Data;
// using Microsoft.ML.Transforms;
// using Microsoft.ML.Transforms.TimeSeries;
//
// namespace dengue.watch.api.infrastructure.ml;
//
// /// <summary>
// /// Dengue forecasting service using ML.NET
// /// </summary>
// public class DengueForecastService : IPredictionService<DengueForecastInput, DengueForecastOutput>
// {
//     private readonly MLContext _mlContext;
//     private readonly ILogger<DengueForecastService> _logger;
//     private ITransformer? _model;
//     private readonly string _modelPath;
//     private readonly IWebHostEnvironment _hostEnv;
//
//
//     public DengueForecastService(ILogger<DengueForecastService> logger, IWebHostEnvironment hostEnv)
//     {
//         _mlContext = new MLContext(seed: 1);
//         // _mlContext.ComponentCatalog.RegisterAssembly(typeof(IsWetWeekMappingFactory).Assembly);
//
//         
//         _logger = logger;
//         _hostEnv =  hostEnv;
//         _modelPath = Path.Combine(hostEnv.ContentRootPath,"infrastructure", "ml", "models", "DengueForecastModel.zip");
//         // Create models directory if it doesn't exist
//         var modelDir = Path.GetDirectoryName(_modelPath);
//         if (modelDir != null && !Directory.Exists(modelDir))
//         {
//             Directory.CreateDirectory(modelDir);
//         }
//         
//         LoadModelIfExists();
//     }
//
//     public async Task<DengueForecastOutput> PredictAsync(DengueForecastInput input)
//     {
//         if (_model == null)
//         {
//             throw new InvalidOperationException("Model is not trained or loaded. Please train the model first.");
//         }
//
//         var predictionEngine = _mlContext.Model.CreatePredictionEngine<DengueForecastInput, DengueForecastOutput>(_model);
//         
//         return await Task.Run(() =>
//         {
//             var prediction = predictionEngine.Predict(input);
//             // prediction. = input.Location;
//             // prediction.PredictionDate = DateTime.UtcNow;
//             // prediction.ConfidenceLevel = CalculateConfidence(prediction);
//             
//             // _logger.LogInformation("Forecast generated for location {Location} with confidence {Confidence}%", 
//             // input.Location, prediction.ConfidenceLevel * 100);
//             
//             return prediction;
//         });
//     }
//
//     public async Task<IEnumerable<DengueForecastOutput>> PredictBatchAsync(IEnumerable<DengueForecastInput> inputs)
//     {
//         if (_model == null)
//         {
//             throw new InvalidOperationException("Model is not trained or loaded. Please train the model first.");
//         }
//
//         var inputList = inputs.ToList();
//         var predictions = new List<DengueForecastOutput>();
//
//         foreach (var input in inputList)
//         {
//             var prediction = await PredictAsync(input);
//             predictions.Add(prediction);
//         }
//
//         return predictions;
//     }
//
//     public async Task<ModelMetrics> TrainModelAsync()
//     {
//         _logger.LogInformation("Starting model training...");
//         string fullpath = Path.Combine(_hostEnv.ContentRootPath, "infrastructure", "ml", "data", "weekly_training_data.csv");
//
//         var config = new CsvConfiguration(CultureInfo.InvariantCulture)
//         {
//             HasHeaderRecord = true,
//             Delimiter = ",",
//         };
//
//         using var reader = new StreamReader(fullpath);
//         using var csv = new CsvReader(reader, config);
//         var records = csv.GetRecords<DengueForecastInput>().ToList();
//
//         IDataView dataView = _mlContext.Data.LoadFromEnumerable(records);
//
//         // 2. Split data into train/test
//         var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
//
//         // 3. Build pipeline for training
//         var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(
//         outputColumnName: "WeatherCategoryKey",
//         inputColumnName: nameof(DengueForecastInput.DominantWeatherCategory))
//         .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
//             outputColumnName: "WeatherCategoryEncoded",
//             inputColumnName: "WeatherCategoryKey"))
//         // .Append(_mlContext.Transforms.CustomMapping<DengueForecastInput, IsWetWeekTransformed>(
//         //     (input, output) => output.IsWetWeekFloat = input.IsWetWeek?.ToUpper() == "TRUE" ? 1.0f : 0.0f,
//         //     contractName: "IsWetWeekConversion"))
//         // .Append(_mlContext.Transforms.CustomMapping(
//         //     IsWetWeekMappingFactory,
//         //     contractName: "IsWetWeekConversion"))
//         .Append(_mlContext.Transforms.CustomMapping<DengueForecastInput, IsWetWeekTransformed>(
//             (input, output) => output.IsWetWeekFloat = input.IsWetWeek?.ToUpper() == "TRUE" ? 1.0f : 0.0f,
//             "IsWetWeekConversion"))
//         .Append(_mlContext.Transforms.Conversion.ConvertType(
//             outputColumnName: "DengueYearFloat",
//             inputColumnName: nameof(DengueForecastInput.DengueYear),
//             outputKind: DataKind.Single))
//         .Append(_mlContext.Transforms.Conversion.ConvertType(
//             outputColumnName: "DengueWeekNumberFloat",
//             inputColumnName: nameof(DengueForecastInput.DengueWeekNumber),
//             outputKind: DataKind.Single))
//         .Append(_mlContext.Transforms.Conversion.ConvertType(
//             outputColumnName: "LagWeekNumberFloat",
//             inputColumnName: nameof(DengueForecastInput.LagWeekNumber),
//             outputKind: DataKind.Single))
//         // Now concatenate all features
//         .Append(_mlContext.Transforms.Concatenate(
//             "Features",
//             "WeatherCategoryEncoded",
//             "IsWetWeekFloat",
//             "DengueYearFloat",
//             "DengueWeekNumberFloat",
//             "LagWeekNumberFloat",
//             nameof(DengueForecastInput.TemperatureMean),
//             nameof(DengueForecastInput.TemperatureMax),
//             nameof(DengueForecastInput.HumidityMean),
//             nameof(DengueForecastInput.HumidityMax),
//             nameof(DengueForecastInput.PrecipitationMean),
//             nameof(DengueForecastInput.PrecipitationMax)
//         ))
//             .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
//             .Append(_mlContext.Regression.Trainers.FastTree(
//                 labelColumnName: "DengueCount",
//                 featureColumnName: "Features"));
//
//         _model = await Task.Run(() => pipeline.Fit(split.TrainSet));
//         var predictions = _model.Transform(split.TestSet);
//
//         var result = _mlContext.Regression.Evaluate(predictions,
//             labelColumnName: "DengueCount",
//             scoreColumnName: "Score");
//
//         _logger.LogInformation("R^2 : {MetricRSquared}\nRMSE: {RMSE}",
//             result.RSquared.ToString("0.###"),
//             result.RootMeanSquaredError
//         );
//         
//         var predictor = _mlContext.Model.CreatePredictionEngine<DengueForecastInput, DengueForecastOutput>(_model);
//         
//         
//         var sampleData = new DengueForecastInput
//         {
//             TemperatureMean = 27.09f,
//             TemperatureMax = 27.60f,
//             HumidityMean = 87.57f,
//             HumidityMax = 90.00f,
//             PrecipitationMean = 8.49f,
//             PrecipitationMax = 17.50f,
//             DominantWeatherCategory = "Rain",
//             IsWetWeek = "TRUE"
//             // fill other fields as needed
//         };
//
//         var pr = predictor.Predict(sampleData);
//         
//         // string saveFullpath = Path.Combine(_hostEnv.ContentRootPath, "infrastructure", "ml", "models", "DengueForecastModel.zip");
//         
//         await SaveModelAsync();
//
//         // Calculate metrics
//         var metrics = new ModelMetrics(
//             Accuracy: 0.85, // This would be calculated from actual validation
//             MeanAbsoluteError: result.RootMeanSquaredError,
//             RSquared: result.RSquared,
//             TrainedAt: DateTime.UtcNow,
//             TrainingDataSize: records.Count
//         );
//
//         // _logger.LogInformation("Model training completed. Training data size: {Size}, Accuracy: {Accuracy}%", 
//             // dataList.Count, metrics.Accuracy * 100);
//
//         return metrics;
//     }
//
//     public ModelInfo GetModelInfo()
//     {
//         return new ModelInfo(
//             Name: "Dengue Forecast Model",
//             Version: "1.0.0",
//             LastTrained: File.Exists(_modelPath) ? File.GetLastWriteTime(_modelPath) : DateTime.MinValue,
//             IsLoaded: _model != null,
//             Description: "Time series forecasting model for dengue case prediction using SSA (Singular Spectrum Analysis)"
//         );
//     }
//
//     private void LoadModelIfExists()
//     {
//         if (File.Exists(_modelPath))
//         {
//             try
//             {
//                 _model = _mlContext.Model.Load(_modelPath, out _);
//                 _logger.LogInformation("Dengue forecast model loaded successfully from {Path}", _modelPath);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to load dengue forecast model from {Path}", _modelPath);
//             }
//         }
//         else
//         {
//             _logger.LogInformation("No existing dengue forecast model found at {Path}", _modelPath);
//         }
//     }
//
//     private async Task SaveModelAsync()
//     {
//         if (_model != null)
//         {
//             await Task.Run(() =>
//             {
//                 _mlContext.Model.Save(_model, null, _modelPath);
//                 _logger.LogInformation("Dengue forecast model saved to {Path}", _modelPath);
//             });
//         }
//     }
//
//     private static float CalculateConfidence(DengueForecastOutput prediction)
//     {
//         // Simple confidence calculation based on forecast bounds
//         // var avgForecast = prediction.ForecastedCases.Average();
//         // var avgLower = prediction.LowerBoundCases.Average();
//         // var avgUpper = prediction.UpperBoundCases.Average();
//         //
//         // if (avgUpper - avgLower == 0) return 1.0f;
//         //
//         // var confidence = 1.0f - ((avgUpper - avgLower) / Math.Max(avgForecast, 1.0f));
//         return Math.Max(0.0f, Math.Min(1.0f, 1));
//     }
//     
// // Helper
// // [CustomMappingFactoryAttribute("IsWetWeekConversion")]
// // public class IsWetWeekTransformed
// // {
// //     public float IsWetWeekFloat { get; set; }
// // }
// }
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace dengue.watch.api.infrastructure.ml;

/// <summary>
/// Enhanced Dengue forecasting service with probability and confidence intervals
/// </summary>
public class BasicDengueForecastService : BaseDengueForecast, IPredictionService<DengueForecastInput, DengueForecastOutput>
{
    private readonly MLContext _mlContext;
    private readonly ILogger<BasicDengueForecastService> _logger;
    private ITransformer? _model;
    private string _modelPath;
    
    private ITransformer? _advModel;
    private readonly string _advModelPath;
    private readonly string _metricsPath;
    private readonly IWebHostEnvironment _hostEnv;
    private double _standardDeviation;
    private const float OUTBREAK_THRESHOLD = 10f; // Adjust based on your region

    public BasicDengueForecastService(ILogger<BasicDengueForecastService> logger, IWebHostEnvironment hostEnv, MLContext mlContext) : base(mlContext, logger, hostEnv)
    {
        _mlContext = new MLContext(seed: 1);
        _logger = logger;
        _hostEnv = hostEnv;
        _modelPath = Path.Combine(hostEnv.ContentRootPath, "infrastructure", "ml", "models", "DengueForecastModel.zip");
        _metricsPath = Path.Combine(hostEnv.ContentRootPath, "infrastructure", "ml", "models", "model_metrics.json");
        
        
        
        // Create models directory if it doesn't exist
        var modelDir = Path.GetDirectoryName(_modelPath);
        if (modelDir != null && !Directory.Exists(modelDir))
        {
            Directory.CreateDirectory(modelDir);
        }

        LoadModelsIfExist();
    }
    
    private void LoadModelsIfExist()
    {
        LoadModelIfExists(_modelPath, _metricsPath,out _model);
        
        LoadMetricsIfExists(_metricsPath);
    }

    public async Task<DengueForecastOutput> PredictAsync(DengueForecastInput input)
    {
        if (_model == null)
        {
            throw new InvalidOperationException("Model is not trained or loaded. Please train the model first.");
        }

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<DengueForecastInput, DengueForecastOutput>(_model);
        
        return await Task.Run(() =>
        {
            var prediction = predictionEngine.Predict(input);
            
            // Calculate confidence intervals and probability
            EnrichPredictionWithStatistics(prediction, confidenceLevel: 0.95);
            
            _logger.LogInformation(
                "Forecast: {Cases:F1} cases (Range: {Lower:F1}-{Upper:F1}), Outbreak Probability: {Prob:F1}%", 
                prediction.Score,
                prediction.LowerBound,
                prediction.UpperBound,
                prediction.ProbabilityOfOutbreak
            );
            
            return prediction;
        });
    }
    public async Task<IEnumerable<DengueForecastOutput>> PredictBatchAsync(IEnumerable<DengueForecastInput> inputs)
    {
        if (_model == null)
        {
            throw new InvalidOperationException("Model is not trained or loaded. Please train the model first.");
        }

        var inputList = inputs.ToList();
        var predictions = new List<DengueForecastOutput>();

        foreach (var input in inputList)
        {
            var prediction = await PredictAsync(input);
            predictions.Add(prediction);
        }

        return predictions;
    }

    public async Task<ModelMetrics> TrainModelAsync()
    {
        _logger.LogInformation("Starting model training...");
        string fullpath = Path.Combine(_hostEnv.ContentRootPath, "infrastructure", "ml", "data", "weekly_training_data.csv");

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
        };

        using var reader = new StreamReader(fullpath);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<DengueForecastInput>().ToList();

        IDataView dataView = _mlContext.Data.LoadFromEnumerable(records);

        // Split data into train/test
        var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        // Build pipeline 1
        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(
            outputColumnName: "WeatherCategoryKey",
            inputColumnName: nameof(DengueForecastInput.DominantWeatherCategory))
        .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
            outputColumnName: "WeatherCategoryEncoded",
            inputColumnName: "WeatherCategoryKey"))
        .Append(_mlContext.Transforms.CustomMapping<DengueForecastInput, IsWetWeekTransformed>(
            (input, output) => output.IsWetWeekFloat = input.IsWetWeek?.ToUpper() == "TRUE" ? 1.0f : 0.0f,
            "IsWetWeekConversion"))
        .Append(_mlContext.Transforms.Conversion.ConvertType(
            outputColumnName: "DengueYearFloat",
            inputColumnName: nameof(DengueForecastInput.DengueYear),
            outputKind: DataKind.Single))
        .Append(_mlContext.Transforms.Conversion.ConvertType(
            outputColumnName: "DengueWeekNumberFloat",
            inputColumnName: nameof(DengueForecastInput.DengueWeekNumber),
            outputKind: DataKind.Single))
        .Append(_mlContext.Transforms.Conversion.ConvertType(
            outputColumnName: "LagWeekNumberFloat",
            inputColumnName: nameof(DengueForecastInput.LagWeekNumber),
            outputKind: DataKind.Single))
        .Append(_mlContext.Transforms.Concatenate(
            "Features",
            "WeatherCategoryEncoded",
            "IsWetWeekFloat",
            "DengueYearFloat",
            "DengueWeekNumberFloat",
            "LagWeekNumberFloat",
            nameof(DengueForecastInput.TemperatureMean),
            nameof(DengueForecastInput.TemperatureMax),
            nameof(DengueForecastInput.HumidityMean),
            nameof(DengueForecastInput.HumidityMax),
            nameof(DengueForecastInput.PrecipitationMean),
            nameof(DengueForecastInput.PrecipitationMax)
        ))
        .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
        .Append(_mlContext.Regression.Trainers.FastTree(
            labelColumnName: "DengueCount",
            featureColumnName: "Features"));
        
        // Train model
        _model = await Task.Run(() => pipeline.Fit(split.TrainSet));
        var predictions = _model.Transform(split.TestSet);

        // Evaluate model
        var metrics = _mlContext.Regression.Evaluate(predictions,
            labelColumnName: "DengueCount",
            scoreColumnName: "Score");

        // Calculate standard deviation for confidence intervals
        _standardDeviation = CalculateStandardDeviation(predictions);

        _logger.LogInformation(
            "Model Training Complete:\n" +
            "  R²: {RSquared:F4}\n" +
            "  RMSE: {RMSE:F2}\n" +
            "  MAE: {MAE:F2}\n" +
            "  Std Dev: {StdDev:F2}",
            metrics.RSquared,
            metrics.RootMeanSquaredError,
            metrics.MeanAbsoluteError,
            _standardDeviation
        );

        // Test prediction
        var predictor = _mlContext.Model.CreatePredictionEngine<DengueForecastInput, DengueForecastOutput>(_model);
        var sampleData = new DengueForecastInput
        {
            TemperatureMean = 27.09f,
            TemperatureMax = 27.60f,
            HumidityMean = 87.57f,
            HumidityMax = 90.00f,
            PrecipitationMean = 8.49f,
            PrecipitationMax = 17.50f,
            DominantWeatherCategory = "Rain",
            IsWetWeek = "TRUE"
        };

        var testPrediction = predictor.Predict(sampleData);
        EnrichPredictionWithStatistics(testPrediction, 0.95);
        
        _logger.LogInformation(
            "Sample Prediction: {Cases:F1} cases (Range: {Lower:F1}-{Upper:F1}), Probability: {Prob:F1}%",
            testPrediction.Score,
            testPrediction.LowerBound,
            testPrediction.UpperBound,
            testPrediction.ProbabilityOfOutbreak
        );

        // Save model and metrics
        await SaveModelAsync(_model, _modelPath);
        await SaveMetricsAsync(_metricsPath);

        var modelMetrics = new ModelMetrics(
            Accuracy: metrics.RSquared, // R² as accuracy measure
            MeanAbsoluteError: metrics.MeanAbsoluteError,
            RSquared: metrics.RSquared,
            TrainedAt: DateTime.UtcNow,
            TrainingDataSize: records.Count
        );

        return modelMetrics;
    }


    public ModelInfo GetModelInfo()
    {
        return new ModelInfo(
            Name: "Dengue Forecast Model",
            Version: "1.0.0",
            LastTrained: File.Exists(_modelPath) ? File.GetLastWriteTime(_modelPath) : DateTime.MinValue,
            IsLoaded: _model != null,
            Description: "Regression model for dengue case prediction with confidence intervals and outbreak probability"
        );
    }

}

// public class IsWetWeekTransformed
// {
//     public float IsWetWeekFloat { get; set; }
// }