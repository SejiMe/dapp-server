using dengue.watch.api.features.weatherpooling.models;
using Google.FlatBuffers;
using openmeteo_sdk;

namespace dengue.watch.api.features.weatherpooling.services;

public class WeatherDataAPI : IWeatherDataAPI
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherDataAPI> _logger;
    public WeatherDataAPI(IHttpClientFactory httpClientFactory, ILogger<WeatherDataAPI> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OpenMeteo");
        _logger = logger;
    }

    public async Task<WeatherForecastResponse> GetForecastDataAsync(decimal latitude, decimal longitude)
    {
        var url = $"v1/forecast?" +
        $"latitude={latitude}&longitude={longitude}" +
        $"&daily=weather_code,precipitation_sum,precipitation_hours,rain_sum,relative_humidity_2m_mean,temperature_2m_mean" +
        $"&timezone=Asia%2FSingapore" +
        $"&past_days=1" +
        "&format=json";

        _logger.LogInformation("Fetching weather data for coordinates: {Latitude}, {Longitude}", latitude, longitude);

        try
        {
            var response = await _httpClient.GetFromJsonAsync<WeatherForecastResponse>(url);
            // Use EnsureSuccessStatusCode (throws on any non-2xx)
            

            return response;
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("400"))
        {
            throw new ValidationException("Invalid coordinates or parameters provided");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("429"))
        {
            throw new InvalidOperationException("API rate limit exceeded. Please try again later.", ex);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("500"))
        {
            throw new InvalidOperationException("Open-Meteo service is temporarily unavailable.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to retrieve weather data: {ex.Message}", ex);
        }
    }

    public async Task<ByteBuffer> GetForecastDataAsByteBufferAsync(decimal latitude, decimal longitude)
    {

        var url = $"v1/forecast?" +
        $"latitude={latitude}&longitude={longitude}" +
        $"&daily=weather_code,precipitation_sum,precipitation_hours,rain_sum,relative_humidity_2m_mean,temperature_2m_mean" +
        $"&timezone=Asia%2FSingapore" +
        $"&past_days=1" +
        "&format=flatbuffers";

        _logger.LogInformation("Fetching weather data for coordinates: {Latitude}, {Longitude}", latitude, longitude);

        try
        {
            var response = await _httpClient.GetByteArrayAsync(url);
            // Use EnsureSuccessStatusCode (throws on any non-2xx)
            var buffer = new ByteBuffer(response);

            return buffer;
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("400"))
        {
            throw new ValidationException("Invalid coordinates or parameters provided");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("429"))
        {
            throw new InvalidOperationException("API rate limit exceeded. Please try again later.", ex);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("500"))
        {
            throw new InvalidOperationException("Open-Meteo service is temporarily unavailable.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to retrieve weather data: {ex.Message}", ex);
        }
    }

    public Task<ByteBuffer> GetHistoricalDataAsync(decimal latitude, decimal longitude)
    {
        return Task.FromResult(new ByteBuffer(new byte[0]));
    }
}