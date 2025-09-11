

using Microsoft.AspNetCore.Mvc;

namespace dengue.watch.api.features.weatherpooling.endpoints;


public class GetDailyWeatherDataPooled : IEndpoint
{

    private record WeatherPooledData(string PsgcCode, string AdministrativeAreaName, DateTime Date, int WeatherCode, string WeatherCodeDescription, double PrecipitationSum, double RelativeHumidityMean, double TemperatureMean);
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weatherpooling")
            .WithTags("Weather Pooling")
            .WithOpenApi();

        group.MapGet("/dailyweather", GetWeatherPooledData)
        .WithName("GetWeatherPooledData")
        .WithSummary("Get daily weather data pooled")
        .Produces<IEnumerable<WeatherPooledData>>();
        return app;
    }

    private static async Task<IResult> GetWeatherPooledData([FromServices] ApplicationDbContext _db)
    {
        var dateToCheck = DateTime.SpecifyKind(DateTime.Now.Date.AddDays(-1), DateTimeKind.Utc);

        var weatherData = await _db.DailyWeather
        .Where(x => x.Date == dateToCheck)
        .OrderBy(x => x.AdministrativeArea!.Name)
        .Select(x => new WeatherPooledData(x.PsgcCode, x.AdministrativeArea!.Name, x.Date, x.WeatherCodeId, $"{x.WeatherCode!.MainDescription} {x.WeatherCode!.SubDescription}", x.Precipitation, x.Humidity, x.Temperature))
        .ToListAsync();

        return Results.Ok(weatherData);
    }
}
