using dengue.watch.api.features.weatherpooling.models;
using Google.FlatBuffers;

namespace dengue.watch.api.features.weatherpooling.services;

public interface IWeatherDataAPI
{
    Task<ByteBuffer> GetForecastDataAsByteBufferAsync(decimal latitude, decimal longitude);
    // Treat it as Command Pattern here as for a request object
    Task<WeatherForecastResponse> GetForecastDataAsync(decimal latitude, decimal longitude);

    Task<ByteBuffer> GetHistoricalDataAsync(decimal latitude, decimal longitude);

}
