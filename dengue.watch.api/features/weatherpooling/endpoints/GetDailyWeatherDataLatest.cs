
namespace dengue.watch.api.features.weatherpooling.endpoints
{
    public class GetDailyWeatherDataLatest : IEndpoint
    {
        private record WeatherPooledDataLatest(string PsgcCode, string AdministrativeAreaName, DateTime Date, int WeatherCode, string WeatherCodeDescription, double PrecipitationSum, double RelativeHumidityMean, double TemperatureMean);
        public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("weatherpooling")
                .WithTags("Weather Pooling")
                .WithOpenApi();

            group.MapGet("/daily-weather/latest", GetWeatherPooledData)
            .WithName("GetWeatherPooledDataLatest")
            .WithSummary("Get daily weather data pooled Latest")
            .Produces<IEnumerable<WeatherPooledDataLatest>>();

            return group;
        }

        

        private static async Task<IResult> GetWeatherPooledData([FromServices] ApplicationDbContext _db)
        {

            var weatherData = await _db.DailyWeather
            .OrderBy(x => x.AdministrativeArea!.Name)
            .ThenByDescending(x => x.Date)
            .Take(500)
            .Select(x => new WeatherPooledDataLatest(x.PsgcCode, x.AdministrativeArea!.Name, x.Date, x.WeatherCodeId, $"{x.WeatherCode!.MainDescription} {x.WeatherCode!.SubDescription}", x.Precipitation, x.Humidity, x.Temperature))
            .ToListAsync();

            return Results.Ok(weatherData);
        }
    }
}





