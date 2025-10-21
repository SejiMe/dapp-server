

using Microsoft.AspNetCore.Mvc;

namespace dengue.watch.api.features.weatherpooling.endpoints;


public class GetDailyWeatherDataPerArea : IEndpoint
{

    private record WeatherPooledData(string PsgcCode, string AdministrativeAreaName, DateTime Date, int WeatherCode, string WeatherCodeDescription, double PrecipitationSum, double RelativeHumidityMean, double TemperatureMean);

    private record DailyWeatherRequest(string psgccode, DateOnly? startDate, DateOnly? endDate);
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weatherpooling")
            .WithTags("Weather Pooling")
            .WithOpenApi();

        group.MapGet("/dailyweather/{psgccode}", GetWeatherPooledData)
        .WithName("GetWeatherPooledDataPerArea")
        .WithSummary("Per Area Weather Pooled Data")
        .Produces<IEnumerable<WeatherPooledData>>();
        return app;
    }

    private static async Task<IResult> GetWeatherPooledData(string psgccode, [FromQuery]DateOnly startDate, [FromQuery]DateOnly endDate,[FromServices] ApplicationDbContext _db)
    {
        DateTime dateMinus2 = DateTime.Now.AddDays(-2);
        var dt1 = DateTime.SpecifyKind(startDate.ToDateTime(new TimeOnly(0,0,0)), DateTimeKind.Utc);
        var dt2 =  DateTime.SpecifyKind(endDate.ToDateTime(new TimeOnly(0,0,0)), DateTimeKind.Utc);
        
        if(dt1 > dt2 && (dt1 > dateMinus2 || dt2 > dateMinus2 ))
         throw new ValidationException("Psgc Code does not exist");

        var administrativeAreas = await _db.AdministrativeAreas.Select(x => x.PsgcCode).ToListAsync();

        var hasPsgc = administrativeAreas.Contains(psgccode);
        if (!hasPsgc)
            throw new ValidationException("Psgc Code does not exist");

        var weatherData = await _db.DailyWeather
        .Where(x => x.Date >= dt1 && x.Date <= dt2 && x.PsgcCode == psgccode)
        .OrderBy(x => x.AdministrativeArea!.Name)
        .Select(x => new WeatherPooledData(x.PsgcCode, x.AdministrativeArea!.Name, x.Date, x.WeatherCodeId, $"{x.WeatherCode!.MainDescription} {x.WeatherCode!.SubDescription}", x.Precipitation, x.Humidity, x.Temperature))
        .ToListAsync();

        return Results.Ok(weatherData);
    }
}
