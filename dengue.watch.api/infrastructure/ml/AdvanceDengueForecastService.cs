using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace dengue.watch.api.infrastructure.ml;

public class AdvanceDengueForecastService : BaseDengueForecast, IPredictionService<AdvDengueForecastInput, DengueForecastOutput>
{
    
    private readonly MLContext _mlContext;
    private readonly ILogger<BasicDengueForecastService> _logger;
    private ITransformer? _model;
    private readonly string _modelPath;
    
    private readonly IWebHostEnvironment _hostEnv;
    private readonly string _metricsPath;


    public AdvanceDengueForecastService(ILogger<BasicDengueForecastService> logger, IWebHostEnvironment hostEnv,
        MLContext mlContext) : base(mlContext, logger, hostEnv)
    {
        _mlContext = new MLContext(seed: 1);
        _logger = logger;
        _hostEnv = hostEnv;
        _modelPath = Path.Combine(hostEnv.ContentRootPath, "infrastructure", "ml", "models", "AdvDengueForecastModel.zip");
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
    public async Task<DengueForecastOutput> PredictAsync(AdvDengueForecastInput input)
    {
        if (_model == null)
        {
            throw new InvalidOperationException("Model is not trained or loaded. Please train the model first.");
        }

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<AdvDengueForecastInput, DengueForecastOutput>(_model);
        
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

    public async Task<IEnumerable<DengueForecastOutput>> PredictBatchAsync(IEnumerable<AdvDengueForecastInput> inputs)
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
        string fullpath = Path.Combine(_hostEnv.ContentRootPath, "infrastructure", "ml", "data", "adv-weekly-training-data.csv");

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
        };

        using var reader = new StreamReader(fullpath);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<AdvDengueForecastInput>().ToList();

        IDataView dataView = _mlContext.Data.LoadFromEnumerable(records);

        // Split data into train/test
        var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        // Build pipeline 1
        
        var pipeline2 = _mlContext.Transforms.Conversion.MapValueToKey(
                outputColumnName: "PsgcCodeKey",
                inputColumnName: nameof(AdvDengueForecastInput.PsgcCode))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                outputColumnName: "PsgcCodeEncoded",
                inputColumnName: "PsgcCodeKey")) 
            .Append(_mlContext.Transforms.Conversion.MapValueToKey(
                outputColumnName: "WeatherCategoryKey",
                inputColumnName: nameof(AdvDengueForecastInput.DominantWeatherCategory)))
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
        _model = await Task.Run(() => pipeline2.Fit(split.TrainSet));
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
        var predictor = _mlContext.Model.CreatePredictionEngine<AdvDengueForecastInput, DengueForecastOutput>(_model);
        var sampleData = new AdvDengueForecastInput
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
            Name: "Advance Dengue Forecast Model",
            Version: "1.0.0",
            LastTrained: File.Exists(_modelPath) ? File.GetLastWriteTime(_modelPath) : DateTime.MinValue,
            IsLoaded: _model != null,
            Description: "Regression model for dengue case prediction with confidence intervals and outbreak probability with Geospatial Capability"
        );
    }
}