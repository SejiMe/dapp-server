using dengue.watch.api.features.weatherpooling.services;
using Microsoft.AspNetCore.Mvc;

namespace dengue.watch.api.features.weatherpooling.endpoints;


public class GetHistoricalWeatherLong : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weatherpooling")
            .WithTags("Weather Pooling")
            .WithOpenApi();

        group.MapGet("/historicalweatherlong", GetHistoricalWeatherLongData)
        .WithName("GetHistoricalWeatherLong")
        .WithSummary("Get historical weather data for a long period")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status500InternalServerError);
        return app;
    }

    private static async Task<IResult> GetHistoricalWeatherLongData(
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromServices] IWeatherDataAPI weatherDataAPI,
        [FromServices] ApplicationDbContext _db,
        [FromServices] ILogger<GetHistoricalWeatherLong> _logger,
        CancellationToken cancellationToken)
    {
        // Set start and end date
        if (endDate < startDate)
            throw new ValidationException("End Date must be greater than the Start Date");
        // Get psgc code based on latitude and longitude
        var psgcCode = await _db.AdministrativeAreas
        .Where(x => x.Latitude == latitude && x.Longitude == longitude)
        .Select(x => x.PsgcCode)
        .FirstOrDefaultAsync();
        // Validation: throw exception if PSGC code not found
        if (psgcCode == null)
            throw new NotFoundException("PSGC code not found");

    
        try
        {
            var weatherDatas = await weatherDataAPI.GetHistoricalLongDataAsync(latitude, longitude, cancellationToken, startDate, endDate);
            int count = 0, maxCount = weatherDatas.Daily.Time.Count;

            // Loop through each day and insert data into database  
            while (count < maxCount)
            {
                var weatherDate = weatherDatas.Daily.Time[count];   
                var weatherCode = weatherDatas.Daily.WeatherCode[count];
                var dateToCheck = DateTime.SpecifyKind(weatherDate, DateTimeKind.Utc);

                // Validation: skip if record exists for date and PSGC
                var exists = await _db.DailyWeather
                    .AnyAsync(x => x.Date == dateToCheck && x.PsgcCode == psgcCode);
                
                if (exists)
                {
                    _logger.LogInformation("Daily weather already exists for {Date} {Psgc}", weatherDate.Date, psgcCode);
                    count++;
                    continue;
                }

                var precipitationSum = weatherDatas.Daily.PrecipitationSum[count];
                var relativeHumidityMean = weatherDatas.Daily.RelativeHumidity2mMean[count];
                var temperatureMean = weatherDatas.Daily.Temperature2mMean[count];
                var weatherData = new DailyWeather
                {
                    Date = dateToCheck,
                    WeatherCodeId = weatherCode,
                    PsgcCode = psgcCode,
                    Precipitation = (float)precipitationSum,
                    Temperature = (float)temperatureMean,
                    Humidity = (float)relativeHumidityMean,

                };
                _db.DailyWeather.Add(weatherData);
                count++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (System.Exception)
        {
            return Results.Problem(statusCode: 500, title: "Internal Server Error");
            throw;
        }

        return Results.Ok();
    }
}