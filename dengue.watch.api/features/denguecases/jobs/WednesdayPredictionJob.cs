using System.Globalization;
using dengue.watch.api.common.helpers;
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
            

            
            var dictData = await _db.PredictedWeeklyDengues.Where(a =>
                    a.PredictedIsoWeek == dateParts.ISOWeek &&
                    a.PredictedIsoYear == dateParts.ISOYear)
                .ToDictionaryAsync(title => title.PsgcCode, content => content );
            
            foreach (string psgcCode in barangays)
            {

                // If data does exist already
                if (dictData.TryGetValue(psgcCode, out var predictedDengue))
                {
                    var snapShot = await _aggregatedWeeklyRepository.GetWeeklyHistoricalWeatherSnapshotAsync(psgcCode,dateParts.LaggedYear, dateParts.LaggedWeek, cancellationToken);
                    
                    AdvDengueForecastInput advForecastInput = new()
                    {
                        PsgcCode = psgcCode,
                        TemperatureMean = (float)snapShot.Temperature.Mean,
                        TemperatureMax = (float)snapShot.Temperature.Max,
                        HumidityMean = (float)snapShot.Humidity.Mean,
                        HumidityMax = (float)snapShot.Humidity.Max,
                        PrecipitationMean = (float)snapShot.Precipitation.Mean,
                        PrecipitationMax = (float)snapShot.Precipitation.Max,
                        IsWetWeek = snapShot.IsWetWeek ? "TRUE" : "FALSE",
                        DominantWeatherCategory = snapShot.DominantWeatherCategory,
                    };
                    
                    
                    var updateVal = await _predictionEngine.PredictAsync(advForecastInput);
                    
                    predictedDengue.LaggedIsoWeek =  dateParts.LaggedWeek;
                    predictedDengue.LaggedIsoYear = dateParts.LaggedYear;
                    predictedDengue.PredictedValue = Convert.ToInt32(Math.Round(Convert.ToDecimal(updateVal.Score), 2));
                    predictedDengue.LowerBound = updateVal.LowerBound;
                    predictedDengue.UpperBound = updateVal.UpperBound;
                    predictedDengue.ConfidencePercentage = updateVal.ConfidencePercentage;
                    predictedDengue.ProbabilityOfOutbreak = updateVal.ProbabilityOfOutbreak;
                    predictedDengue.RiskLevel = updateVal.GetRiskLevel();
                    
                    continue;
                }
                
                var fetchedSnapshot = await _aggregatedWeeklyRepository.GetWeeklyHistoricalWeatherSnapshotAsync(psgcCode,dateParts.LaggedYear, dateParts.LaggedWeek, cancellationToken);

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
                    RiskLevel = val.GetRiskLevel(),
                    MonthName = IsoWeekHelper.GetMonthNameFromIsoWeek(dateParts.ISOYear, dateParts.ISOWeek)
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