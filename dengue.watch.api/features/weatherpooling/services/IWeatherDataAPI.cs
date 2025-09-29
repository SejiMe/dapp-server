using dengue.watch.api.features.weatherpooling.models;
using Google.FlatBuffers;

namespace dengue.watch.api.features.weatherpooling.services;

public interface IWeatherDataAPI
{
    Task<WeatherHistoricalResponse> GetHistoricalDataAsync(decimal latitude, decimal longitude,CancellationToken cancellationToken, DateOnly? date);

    Task<WeatherHistoricalResponse> GetHistoricalLongDataAsync(
        decimal latitude, decimal longitude, CancellationToken cancellationToken,
        DateOnly startDate, DateOnly endDate);

}
