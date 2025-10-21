using System.Globalization;
using Microsoft.Extensions.ML;
using Microsoft.ML;
using Npgsql.PostgresTypes;

namespace dengue.watch.api.features.denguecases.services;


public interface IPredictDengueCaseService
{
    public void PredictWeeklyMinusOneHistoricalWeatherData(string psgcCode);
}

public class PredictDengueCaseService(
    ILogger<PredictDengueCaseService> _logger,
    PredictionEnginePool<DengueForecastInput, DengueForecastOutput> _predictionEngine,
    IAggregatedWeeklyHistoricalWeatherRepository _weeklyHistoricalWeatherRepository
    ) : IPredictDengueCaseService
{
    // This either throw an error or Logs a success
    // This will only be run


    public void PredictWeeklyMinusOneHistoricalWeatherData(string psgcCode)
    {
        DateTime weeklyDataDate = DateTime.Now.AddDays(-14);

        DayOfWeek currentDayOfWeek = weeklyDataDate.DayOfWeek;
        
        if(currentDayOfWeek != DayOfWeek.Wednesday)
            throw new InvalidOperationException("We can't Process these dates");

        int IsoWeek = ISOWeek.GetWeekOfYear(weeklyDataDate);
        int IsoYear = ISOWeek.GetYear(weeklyDataDate);
       
        /* string psgcCode,
            IReadOnlyCollection<int> dengueYears,
            int? dengueWeekNumber,
            (int From, int To)? dengueWeekRange,
            CancellationToken cancellationToken = default */
        // Get Data from repository
        //_weeklyHistoricalWeatherRepository.GetWeeklySnapshotsAsync(psgcCode, );

        // TODO implement
        // var res = _predictionEngine.Predict();
        throw new NotImplementedException();
    }
    

}