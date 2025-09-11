using dengue.watch.api.features.weatherpooling.models;
using dengue.watch.api.features.weatherpooling.services;
using dengue.watch.api.infrastructure.database;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace dengue.watch.api.features.weatherpooling.jobs;

public class DailyWeatherPoolingJob : IJob
{
    private readonly ILogger<DailyWeatherPoolingJob> _logger;
    private readonly IWeatherDataAPI _weatherDataApi;
    private readonly WeatherDataProcessor _processor;
    private readonly ApplicationDbContext _db;

    public DailyWeatherPoolingJob(
        ILogger<DailyWeatherPoolingJob> logger,
        IWeatherDataAPI weatherDataApi,
        WeatherDataProcessor processor,
        ApplicationDbContext db)
    {
        _logger = logger;
        _weatherDataApi = weatherDataApi;
        _processor = processor;
        _db = db;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting DailyWeatherPoolingJob at {Time}", DateTimeOffset.UtcNow);

        // Example barangay list with PSGC, lat, long
        
        var barangays = await _db.AdministrativeAreas
        .Where(x => x.GeographicLevel == "Bgy" && x.Latitude.HasValue && x.Longitude.HasValue)
        .Select(x => new WeatherForecastRequest(x.PsgcCode, (decimal)x.Latitude!, (decimal)x.Longitude!))
        .ToListAsync();

        foreach (var (psgc, lat, lon) in barangays)
        {
            try
            {

                var apiResponse = await _weatherDataApi.GetForecastDataAsync(lat, lon);
                var dayMinus1 = _processor.GetDayMinus1Data(apiResponse);
                dayMinus1.FK_PsgcCode = psgc;
                var dateToCheck = DateTime.SpecifyKind(dayMinus1.Date, DateTimeKind.Utc);
                // Validation: skip if record exists for date and PSGC
                var exists = await _db.DailyWeather
                    .AnyAsync(x => x.Date == dateToCheck && x.PsgcCode == psgc);
                if (exists)
                {
                    _logger.LogInformation("Daily weather already exists for {Date} {Psgc}", dayMinus1.Date, psgc);
                    continue;
                }

                // Map to persistence entity
                var entity = new DailyWeather
                {
                    Date = dateToCheck,
                    PsgcCode = psgc,
                    WeatherCodeId = dayMinus1.WeatherCode,
                    Temperature = (float)dayMinus1.TemperatureMean,
                    Precipitation = (float)dayMinus1.PrecipitationSum,
                    Humidity = (float)dayMinus1.RelativeHumidityMean
                };

                _db.DailyWeather.Add(entity);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing weather data for {Psgc}", psgc);
            }
        }

        _logger.LogInformation("Finished DailyWeatherPoolingJob at {Time}", DateTimeOffset.UtcNow);
    }
}


