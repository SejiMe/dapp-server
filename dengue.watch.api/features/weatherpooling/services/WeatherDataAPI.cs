using dengue.watch.api.features.weatherpooling.models;
using Google.FlatBuffers;
using openmeteo_sdk;

namespace dengue.watch.api.features.weatherpooling.services;

public class WeatherDataAPI : IWeatherDataAPI
{

    private readonly HttpClient _httpClientArchive;
    private readonly ILogger<WeatherDataAPI> _logger;
    
    private const string DateFormat = "yyyy-MM-dd";
    public WeatherDataAPI(IHttpClientFactory httpClientFactory, ILogger<WeatherDataAPI> logger)
    {
        _httpClientArchive = httpClientFactory.CreateClient("OpenMeteoArchive");
        _logger = logger;

    }
    /// <summary>
    ///  This method is used to get the historical weather data for a given date
    /// </summary>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <param name="date"></param>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <param name="date"></param>
    /// <returns></returns>
    public async Task<WeatherHistoricalResponse> GetHistoricalDataAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken, DateOnly? date)
    {
        DateOnly startDate = date ?? DateOnly.FromDateTime(DateTime.Now.AddDays(-2));
        DateOnly endDate = date ?? DateOnly.FromDateTime(DateTime.Now.AddDays(-2));

        

        string url = $"v1/archive?" +
        $"latitude={latitude}&longitude={longitude}" +
        $"&start_date={startDate.ToString(DateFormat)}&end_date={endDate.ToString(DateFormat)}" +
        $"&daily=weather_code,precipitation_sum,precipitation_hours,rain_sum,relative_humidity_2m_mean,temperature_2m_mean" +
        $"&timezone=Asia%2FSingapore" +
        "&format=json";
        
        
        try
        {
            WeatherHistoricalResponse response = await _httpClientArchive.GetFromJsonAsync<WeatherHistoricalResponse>(url, cancellationToken) ?? throw new ValidationException("Invalid coordinates or parameters provided");
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

    public async Task<WeatherHistoricalResponse> GetHistoricalLongDataAsync(decimal latitude, decimal longitude,CancellationToken cancellationToken, DateOnly startDate, DateOnly endDate )
    {
        var url = $"v1/archive?" +
        $"latitude={latitude}&longitude={longitude}" +
        $"&start_date={startDate.ToString("yyyy-MM-dd")}&end_date={endDate.ToString("yyyy-MM-dd")}" +
        $"&daily=weather_code,precipitation_sum,precipitation_hours,rain_sum,relative_humidity_2m_mean,temperature_2m_mean" +
        $"&timezone=Asia%2FSingapore" +
        "&format=json";

        _logger.LogInformation("Fetching weather data for coordinates: {Latitude}, {Longitude}", latitude, longitude);

        try
        {
            var response = await _httpClientArchive.GetFromJsonAsync<WeatherHistoricalResponse>(url, cancellationToken);
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
}