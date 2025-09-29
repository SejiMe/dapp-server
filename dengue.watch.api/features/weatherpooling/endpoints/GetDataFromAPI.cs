using dengue.watch.api.features.weatherpooling.services;
using Google.FlatBuffers;
using Microsoft.AspNetCore.Mvc;

namespace dengue.watch.api.features.weatherpooling.endpoints;

public class GetDataFromAPI : IEndpoint
{

    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weatherpooling")
            .WithTags("Weather Pooling")
            .WithOpenApi();

        group.MapGet("/forecast", DataFetchingHandler);

        return group;
    }

    private static async Task<IResult> DataFetchingHandler(
        [FromServices] IWeatherDataAPI weatherDataAPI,
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        [FromQuery] DateOnly? date,
        // [FromServices] WeatherDataProcessor weatherDataProcessor,
        CancellationToken cancellationToken)
    {
        var data = await weatherDataAPI.GetHistoricalDataAsync(latitude, longitude, cancellationToken, date);

        var dataRaw = data;
        return Results.Ok(dataRaw);
    }
}