using System.Globalization;
using Microsoft.Extensions.ML;

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

    // This will only be run


    public void PredictWeeklyMinusOneHistoricalWeatherData(string psgcCode)
    {
        DateTime weeklyDataDate = DateTime.Now.AddDays(-14);

        DayOfWeek currentDayOfWeek = weeklyDataDate.DayOfWeek;
        
        if(currentDayOfWeek != DayOfWeek.Wednesday)
            throw new InvalidOperationException("We can't Process these dates");

        int IsoWeek = ISOWeek.GetWeekOfYear(weeklyDataDate);
        int IsoYear = ISOWeek.GetYear(weeklyDataDate);
        
        // FIXME fix this



        /* string psgcCode,
            IReadOnlyCollection<int> dengueYears,
            int? dengueWeekNumber,
            (int From, int To)? dengueWeekRange,
            CancellationToken cancellationToken = default */
        // Get Data from repository
        //_weeklyHistoricalWeatherRepository.GetWeeklySnapshotsAsync(psgcCode, );

        /*
            *                TODO implement
            * var res = _predictionEngine.Predict();
        */
        
        
        // TODO
        // Work
        throw new NotImplementedException();
    }
    

}