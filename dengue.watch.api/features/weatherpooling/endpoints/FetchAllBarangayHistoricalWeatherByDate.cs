using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dengue.watch.api.features.weatherpooling.models;
using dengue.watch.api.features.weatherpooling.services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.weatherpooling.endpoints
{
    public class FetchAllBarangayHistoricalWeatherByDate : IEndpoint
    {
        public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("weatherpooling")
            .WithTags("Weather Pooling")
            .WithSummary("Fetch weather history all barangay by Date");

            group.MapPost("fetch-all", HandleAsync);

            return group; 
        }

        public record ErrorDetail(string psgcCode, string errorType, string message);
        public record Response(
            int successCount,
            int failedCount,
            int existCount,
            List<string> existingPsgcCodes,
            List<ErrorDetail> errors);

        public record Request(DateTime date);

        private static async ValueTask<IResult> HandleAsync([FromBody] Request request, [FromServices] IWeatherDataAPI _weatherDataApi, [FromServices] WeatherDataProcessor  _processor, [FromServices] ApplicationDbContext _db, [FromServices] ILogger<FetchAllBarangayHistoricalWeatherByDate> _logger, CancellationToken ctk = default)
        {
            _logger.LogInformation("Starting Fetch 1 day Historical Weather Data at {Time}", DateTimeOffset.UtcNow);
        
        var cancellationToken = ctk;
        
        // Get All Barangays
        var barangays = await _db.AdministrativeAreas
        .Where(x => x.GeographicLevel == "Bgy" && x.Latitude.HasValue && x.Longitude.HasValue)
        .Select(x => new WeatherHistoricalRequest(x.PsgcCode, (decimal)x.Latitude!, (decimal)x.Longitude!))
        .ToListAsync(cancellationToken);

        int success = 0, failed = 0, existing = 0;
        List<string> existingPsgcCodes = [];
        List<ErrorDetail> errors = [];

        foreach (var (psgc, lat, lon) in barangays)
        {
            try
            {
                // eto yung nag eextract ng data sa may Open Meteo API

                var apiResponse = await _weatherDataApi.GetHistoricalDataAsync(lat, lon, cancellationToken, DateOnly.FromDateTime(request.date) );
                var dayData = _processor.Get1DayData(apiResponse);
                dayData.FK_PsgcCode = psgc;
                var dateToCheck = DateTime.SpecifyKind(dayData.Date, DateTimeKind.Utc);
                
                // Validation: skip if record exists for date and PSGC
                var exists = await _db.DailyWeather
                    .AnyAsync(x => x.Date == dateToCheck && x.PsgcCode == psgc, cancellationToken);

                if (exists)
                {
                    _logger.LogInformation("Daily weather already exists for {Date} {Psgc}", dayData.Date, psgc);
                    existing++;
                    existingPsgcCodes.Add(psgc);
                    continue;
                }

                // Map to persistence entity
                var entity = new DailyWeather
                {
                    Date = dateToCheck,
                    PsgcCode = psgc,
                    WeatherCodeId = dayData.WeatherCode,
                    Temperature = (float)dayData.TemperatureMean,
                    Precipitation = (float)dayData.PrecipitationSum,
                    Humidity = (float)dayData.RelativeHumidityMean
                };

                _db.DailyWeather.Add(entity);
                success++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Failed processing weather data for {Psgc} \ndetails:\n\t{Message}", psgc, ex.Message);
                errors.Add(new ErrorDetail(psgc, ex.GetType().Name, ex.Message));
            }
        }

            try
            {
                await _db.SaveChangesAsync();
                _logger.LogInformation("Finished DailyWeatherPoolingJob at {Time}", DateTimeOffset.UtcNow);
                Response response = new(
                    successCount: success,
                    failedCount: failed,
                    existCount: existing,
                    existingPsgcCodes: existingPsgcCodes,
                    errors: errors);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save weather data to database");
                return Results.Problem("Failed to save weather data to database");
            }
        }
    }
}