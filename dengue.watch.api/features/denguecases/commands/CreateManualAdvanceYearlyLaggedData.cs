using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.denguecases.commands;

public class CreateManualAdvanceYearlyLaggedData : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithSummary("Manual Advance Prediction by Psgc and Date (yyyy-MM-dd) based on a yearly")
            .WithTags("Dengue Cases");

        group.MapPost("advance/year-minus-one", Handler)
            .Produces<IResult>();
        return group;
    }
    
    public record CreateDenguePredictionRequest(string psgccode, DateOnly dt);
    public record CreateDenguePredictionResponse(string psgccode, string barangayName, int iso_year, int iso_week, int lagged_week, int lagged_year ,float value_predicted, double outbreak_probability);
    private static async Task<Results<Created<CreateDenguePredictionResponse>, StatusCodeHttpResult ,Conflict<string>, BadRequest, ProblemHttpResult>> Handler(
        CreateDenguePredictionRequest _request,
        [FromServices] DateExtraction _dateExtraction,
        [FromServices] ApplicationDbContext _db,
        [FromServices] IAggregatedWeeklyHistoricalWeatherRepository _repository,
        // [FromServices] PredictionEnginePool<DengueForecastInput, DengueForecastOutput> _predictionEngine,
        [FromServices] IPredictionService<AdvDengueForecastInput, DengueForecastOutput> _predictionEngine,
        CancellationToken cancellation = default)
    {
        try
        {
            var dateParts = _dateExtraction.ExtractCurrentDateAndLaggedDateYearMinus1(_request.dt);
            
            
            var hasData = _db.PredictedWeeklyDengues.Where(p =>
                p.PsgcCode == _request.psgccode && p.PredictedIsoWeek == dateParts.ISOWeek &&
                p.PredictedIsoYear == dateParts.ISOYear).FirstOrDefault();

            // Last Year Weather Data
            var fetchedSnapshot = await _repository.GetWeeklyHistoricalWeatherSnapshotAsync(_request.psgccode,dateParts.LaggedYear, dateParts.LaggedWeek, cancellation);
        
            if (hasData != null)
            {

                AdvDengueForecastInput newForecastInput = new()
                {
                    PsgcCode = _request.psgccode,
                    TemperatureMean = (float)fetchedSnapshot.Temperature.Mean,
                    TemperatureMax = (float)fetchedSnapshot.Temperature.Max,
                    HumidityMean = (float)fetchedSnapshot.Humidity.Mean,
                    HumidityMax = (float)fetchedSnapshot.Humidity.Max,
                    PrecipitationMean = (float)fetchedSnapshot.Precipitation.Mean,
                    PrecipitationMax = (float)fetchedSnapshot.Precipitation.Max,
                    IsWetWeek = fetchedSnapshot.IsWetWeek ? "TRUE" : "FALSE",
                    DominantWeatherCategory = fetchedSnapshot.DominantWeatherCategory,
                };
            
            
                var newValue = await _predictionEngine.PredictAsync(newForecastInput);

                hasData.LaggedIsoWeek =  dateParts.LaggedWeek;
                hasData.LaggedIsoYear = dateParts.LaggedYear;
                hasData.PredictedValue = Convert.ToInt32(Math.Round(Convert.ToDecimal(newValue.Score), 2));
                hasData.LowerBound = newValue.LowerBound;
                hasData.UpperBound = newValue.UpperBound;
                hasData.ConfidencePercentage = newValue.ConfidencePercentage;
                hasData.ProbabilityOfOutbreak = newValue.ProbabilityOfOutbreak;
                hasData.RiskLevel = newValue.GetRiskLevel();

                await _db.SaveChangesAsync();
                
                return TypedResults.StatusCode(StatusCodes.Status205ResetContent);
            }
            
            AdvDengueForecastInput forecastInput = new()
            {
                PsgcCode = _request.psgccode,
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
                RiskLevel = val.GetRiskLevel(),
                MonthName = IsoWeekHelper.GetMonthNameFromIsoWeek(dateParts.ISOYear, dateParts.ISOWeek)
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