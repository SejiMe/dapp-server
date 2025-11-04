using dengue.watch.api.features.denguecases.services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.ML;

namespace dengue.watch.api.features.denguecases.endpoints;

public class CreateManualBasicPredictionByPsgcAndDate : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dengue-cases")
            .WithSummary("Create Manual Prediction by Psgc and Date (yyyy-MM-dd)")
            .WithTags("Dengue Cases");

        group.MapPost("basic", Handler)
            .Produces<IResult>();
        return group;
    }
    
    public record CreateDenguePredictionRequest(string psgccode, DateOnly dt);
    public record CreateDenguePredictionResponse(string psgccode, string barangayName, int iso_year, int iso_week, int lagged_week, int lagged_year ,float valuePredicted, double probability);
    private static async Task<Results<Created<CreateDenguePredictionResponse>,Conflict<string>, BadRequest, ProblemHttpResult>> Handler(
        CreateDenguePredictionRequest _request,
        [FromServices] DateExtraction _dateExtraction,
        [FromServices] ApplicationDbContext _db,
        [FromServices] IAggregatedWeeklyHistoricalWeatherRepository _repository,
        // [FromServices] PredictionEnginePool<DengueForecastInput, DengueForecastOutput> _predictionEngine,
        [FromServices] IPredictionService<DengueForecastInput, DengueForecastOutput> _predictionEngine,
        CancellationToken cancellation = default)
    {
        try
        {
            var dateParts = _dateExtraction.ExtractCurrentDateAndLaggedDate(_request.dt);

            var hasExist = _db.PredictedWeeklyDengues.Where(p =>
                p.PsgcCode == _request.psgccode && p.PredictedIsoWeek == dateParts.ISOWeek &&
                p.PredictedIsoYear == dateParts.ISOYear);

            if (hasExist.Any())
                return TypedResults.Conflict("Resource already exists!");
            
            
            var fetchedSnapshot = await _repository.GetWeeklyHistoricalWeatherSnapshotAsync(_request.psgccode,dateParts.LaggedYear, dateParts.LaggedWeek, cancellation);
            
            DengueForecastInput forecastInput = new()
            {
                TemperatureMean = (float)fetchedSnapshot.Temperature.Mean,
                TemperatureMax = (float)fetchedSnapshot.Temperature.Max,
                HumidityMean = (float)fetchedSnapshot.Humidity.Mean,
                HumidityMax = (float)fetchedSnapshot.Humidity.Max,
                PrecipitationMean = (float)fetchedSnapshot.Precipitation.Mean,
                PrecipitationMax = (float)fetchedSnapshot.Precipitation.Max,
                IsWetWeek = fetchedSnapshot.IsWetWeek ? "TRUE" : "FALSE",
                DominantWeatherCategory = fetchedSnapshot.DominantWeatherCategory,
            };

            var val = await _predictionEngine.PredictAsync(forecastInput);

            PredictedWeeklyDengueCase dCase = new()
            {
                PsgcCode = _request.psgccode,
                LaggedIsoWeek = dateParts.LaggedWeek,
                LaggedIsoYear = dateParts.LaggedYear,
                PredictedIsoWeek = dateParts.ISOWeek,
                PredictedIsoYear = dateParts.ISOYear,
                PredictedValue = Convert.ToInt32(Math.Round(Convert.ToDecimal(val.Score), 2)),
                LowerBound = val.LowerBound,
                UpperBound = val.UpperBound,
                ConfidencePercentage = val.ConfidencePercentage,
                ProbabilityOfOutbreak = val.ProbabilityOfOutbreak,
                RiskLevel = val.GetRiskLevel()
            };


           string bgyName =  _db.AdministrativeAreas.Where(p => p.PsgcCode == _request.psgccode).Select(p => p.Name).Single();
            // check if it exists 
            await _db.PredictedWeeklyDengues.AddAsync(dCase);
            await _db.SaveChangesAsync(cancellation);
            CreateDenguePredictionResponse response = new(_request.psgccode,bgyName, dateParts.ISOYear, dateParts.ISOWeek, dateParts.LaggedWeek, dateParts.LaggedYear, dCase.PredictedValue, dCase.ProbabilityOfOutbreak);
            return TypedResults.Created($"/api/dengue-cases/detailed/{dCase.PredictionId}", response);
        }
        catch (Exception e)
        {
            return TypedResults.Problem($"Cant Create Dengue Prediction Due to {e.Message}. {e.InnerException} {e.StackTrace}");
        }
    }
}