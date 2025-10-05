

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using dengue.watch.api.features.trainingdatapipeline.models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace dengue.watch.api.features.trainingdatapipeline.endpoints;

public class CreateMLRegressionModelEndpoint : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/training-data")
            .WithTags("Training Data Pipeline")
            .WithOpenApi();

        group.MapPost("/api/trainingdatapipeline/createmlregressionmodel", HandleAsync)
            .WithName("CreateRegressionModel")
            .WithSummary("Generate a Regression Machine Learning Model for Prediction");

        return group;
    }



    private static async Task<IResult> HandleAsync(
        [FromBody] CreateMLRegressionModelRequest request,
        [FromServices] IWebHostEnvironment _hostEnv,
        [FromServices] ILogger<CreateMLRegressionModelEndpoint> _logger
        )
    {

        var mlContext = new MLContext(0);
        // Create the regression Model here then as simply implementation
        string fullpath = Path.Combine(_hostEnv.ContentRootPath, "infrastructure", "ml", "data", "weekly_training_data.csv");

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
        };

        using var reader = new StreamReader(fullpath);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<LaggedDengueCausalData>().ToList();

        IDataView dataView = mlContext.Data.LoadFromEnumerable(records);

        var data = mlContext.Data.CreateEnumerable<LaggedDengueCausalData>(dataView, reuseRowObject: false);

        // 2. Split data into train/test
        var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        // 3. Build pipeline for training
        var pipeline = mlContext.Transforms.Conversion.MapValueToKey(
        outputColumnName: "WeatherCategoryKey",
        inputColumnName: nameof(LaggedDengueCausalData.DominantWeatherCategory))
        .Append(mlContext.Transforms.Categorical.OneHotEncoding(
            outputColumnName: "WeatherCategoryEncoded",
            inputColumnName: "WeatherCategoryKey"))
        .Append(mlContext.Transforms.CustomMapping<LaggedDengueCausalData, IsWetWeekTransformed>(
            (input, output) => output.IsWetWeekFloat = input.IsWetWeek?.ToUpper() == "TRUE" ? 1.0f : 0.0f,
            contractName: "IsWetWeekConversion"))
        .Append(mlContext.Transforms.Conversion.ConvertType(
            outputColumnName: "DengueYearFloat",
            inputColumnName: nameof(LaggedDengueCausalData.DengueYear),
            outputKind: DataKind.Single))
        .Append(mlContext.Transforms.Conversion.ConvertType(
            outputColumnName: "DengueWeekNumberFloat",
            inputColumnName: nameof(LaggedDengueCausalData.DengueWeekNumber),
            outputKind: DataKind.Single))
        .Append(mlContext.Transforms.Conversion.ConvertType(
            outputColumnName: "LagWeekNumberFloat",
            inputColumnName: nameof(LaggedDengueCausalData.LagWeekNumber),
            outputKind: DataKind.Single))
        // Now concatenate all features
        .Append(mlContext.Transforms.Concatenate(
            "Features",
            "WeatherCategoryEncoded",
            "IsWetWeekFloat",
            "DengueYearFloat",
            "DengueWeekNumberFloat",
            "LagWeekNumberFloat",
            nameof(LaggedDengueCausalData.TemperatureMean),
            nameof(LaggedDengueCausalData.TemperatureMax),
            nameof(LaggedDengueCausalData.HumidityMean),
            nameof(LaggedDengueCausalData.HumidityMax),
            nameof(LaggedDengueCausalData.PrecipitationMean),
            nameof(LaggedDengueCausalData.PrecipitationMax)
        ))
            .Append(mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(mlContext.Regression.Trainers.FastTree(
                labelColumnName: "Label",
                featureColumnName: "Features"));

        var model = pipeline.Fit(split.TrainSet);
        var predictions = model.Transform(split.TestSet);

        var metrics = mlContext.Regression.Evaluate(predictions,
        labelColumnName: "Label",
        scoreColumnName: "Score");


        _logger.LogInformation("R^2 : {MetricRSquared}\nRMSE: {RMSE}",
            metrics.RSquared.ToString("0.###"),
            metrics.RootMeanSquaredError
        );

          string saveFullpath = Path.Combine(_hostEnv.ContentRootPath, "infrastructure", "ml", "models", "DengueForecastModel.zip");
        mlContext.Model.Save(model, dataView.Schema, saveFullpath);

        var predictor = mlContext.Model.CreatePredictionEngine<LaggedDengueCausalData, DenguePrediction>(model);

        var sampleData = new LaggedDengueCausalData
        {
            TemperatureMean = 27.09f,
            TemperatureMax = 27.60f,
            HumidityMean = 87.57f,
            HumidityMax = 90.00f,
            PrecipitationMean = 8.49f,
            PrecipitationMax = 17.50f,
            DominantWeatherCategory = "Rain",
            IsWetWeek = "TRUE"
            // fill other fields as needed
        };

        var pr = predictor.Predict(sampleData);


        var result = new
        {
            RSquaredValueModel = metrics.RSquared.ToString("0.###"),
            RSME = metrics.RootMeanSquaredError,
            sampleTestData = sampleData,
            predictedTestData = pr
        };

        return Results.Ok(result);
    }

    public record CreateMLRegressionModelRequest();

}

// Helper
public class IsWetWeekTransformed
{
    public float IsWetWeekFloat { get; set; }
}