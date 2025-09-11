using dengue.watch.api.features.weatherpooling.models;

namespace dengue.watch.api.features.weatherpooling.services;

public interface IWeatherDateService
{
    DateTime? ExtractFirstDailyDate(WeatherForecastResponse response);
}

public class WeatherDateService : IWeatherDateService
{
    public DateTime? ExtractFirstDailyDate(WeatherForecastResponse response)
    {
        if (response == null || response.Daily == null || response.Daily.Time == null || response.Daily.Time.Count == 0)
        {
            return null;
        }

        // Expecting the first item to be day minus 1 due to past_days=1
        return response.Daily.Time.First();
    }
}


