using System.Globalization;
using dengue.watch.api.features.denguecases.services;
using Microsoft.Extensions.ML;
using Quartz;

namespace dengue.watch.api.features.denguecases.jobs;

public class WednesdayPredictionJob : IJob
{

    private readonly ILogger<WednesdayPredictionJob> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IAggregatedWeeklyHistoricalWeatherRepository _aggregatedWeeklyRepository;
    private readonly IPredictionService<AdvDengueForecastInput, DengueForecastOutput> _predictionEngine;
    private readonly DateExtraction _dateExtraction;
    public WednesdayPredictionJob(ILogger<WednesdayPredictionJob> logger, ApplicationDbContext db, IAggregatedWeeklyHistoricalWeatherRepository aggregatedWeeklyRepository, IPredictionService<AdvDengueForecastInput, DengueForecastOutput> predictionEngine, DateExtraction dateExtraction)
    {
        _logger = logger;
        _db = db;
        _aggregatedWeeklyRepository = aggregatedWeeklyRepository;
        _predictionEngine = predictionEngine;
        _dateExtraction = dateExtraction;

    }
    public async Task Execute(IJobExecutionContext context)
    {
      

        try
        {
            var cancellationToken = context.CancellationToken;
            _logger.LogInformation("Starting Wednesday Prediction Job");
            var barangays = await _db.AdministrativeAreas
                .Where(x => x.GeographicLevel == "Bgy" && x.Latitude.HasValue && x.Longitude.HasValue)
                .Select(x => x.PsgcCode)
                .ToListAsync(cancellationToken);
            
            var currentDate = DateTime.Now;

            var dateParts = _dateExtraction.ExtractCurrentDateAndLaggedDate(DateOnly.FromDateTime(currentDate));
            var CurrentWeek = dateParts.ISOWeek;
            var CurrentYear = dateParts.ISOYear;
           
            int LagHistoricalWeatherWeek = dateParts.LaggedWeek;
            int LagHistoricalWeatherYear =  dateParts.LaggedYear;
            
            // var res = await _db.PredictedWeeklyDengues.Where( p =>   )
            foreach (string psgcCode in barangays)
            {
                 // Fetch data first if this has already data 
                 
                 
                var fetchedSnapshot = await _aggregatedWeeklyRepository.GetWeeklyHistoricalWeatherSnapshotAsync(psgcCode,LagHistoricalWeatherYear, LagHistoricalWeatherWeek, cancellationToken);

                AdvDengueForecastInput forecastInput = new()
                {
                    PsgcCode = psgcCode,
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
                    PsgcCode = psgcCode,
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
                
                // check if it exists 
                await _db.PredictedWeeklyDengues.AddAsync(dCase);
            }
            await _db.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed Processing Prediction Job");
        }
        
       
        
        _logger.LogInformation("Finished Predicting Values at {Time}", DateTimeOffset.UtcNow);

    }
    
    
    
}