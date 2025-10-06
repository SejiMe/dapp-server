using System.ComponentModel.DataAnnotations;

using dengue.watch.api.features.trainingdatapipeline.models;



namespace dengue.watch.api.features.trainingdatapipeline.endpoints;

public class GetWeeklyTrainingWeather : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/training-data")
            .WithTags("Training Data Pipeline")
            .WithOpenApi();

        group.MapPost("/weekly-weather", HandleAsync)
            .WithName("GetWeeklyTrainingWeather")
            .WithSummary("Get weekly aggregated weather data for training")
            .Produces<WeeklyTrainingWeatherResult>();

        return app;
    }

    private static async Task<IResult> HandleAsync(
        [FromServices] IAggregatedWeeklyHistoricalWeatherRepository repository,
        [FromBody, Required] TrainingDataWeatherRequest request,
        CancellationToken cancellationToken)
    {
        var years = request.Years?.ToArray() ?? Array.Empty<int>();

        if (years.Length == 0)
        {
            return Results.BadRequest("At least one year must be provided.");
        }

        if (request.WeekNumber.HasValue && request.WeekRange is not null)
        {
            return Results.BadRequest("Specify either weekNumber or weekRange, not both.");
        }

        (int From, int To)? range = null;
        if (request.WeekRange is not null)
        {
            range = (request.WeekRange.From, request.WeekRange.To);
        }

        var result = await repository.GetWeeklySnapshotsAsync(
            request.PsgcCode,
            years,
            request.WeekNumber,
            range,
            cancellationToken);

        return Results.Ok(result);
    }

}

