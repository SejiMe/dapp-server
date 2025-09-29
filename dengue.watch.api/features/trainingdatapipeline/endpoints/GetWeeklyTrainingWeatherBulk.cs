using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using dengue.watch.api.common.interfaces;
using dengue.watch.api.features.trainingdatapipeline.models;
using dengue.watch.api.features.trainingdatapipeline.repositories;
using dengue.watch.api.infrastructure.database;
using Microsoft.AspNetCore.Mvc;

namespace dengue.watch.api.features.trainingdatapipeline.endpoints;

public class GetWeeklyTrainingWeatherBulk : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/training-data")
            .WithTags("Training Data Pipeline")
            .WithOpenApi();

        group.MapPost("/weekly-weather/bulk", HandleAsync)
            .WithName("GetWeeklyTrainingWeatherBulk")
            .WithSummary("Get weekly aggregated weather data for all administrative areas")
            .Produces<WeeklyTrainingWeatherResult>();

        return app;
    }

    private static async Task<IResult> HandleAsync(
        [FromServices] IWeeklyTrainingWeatherRepository repository,
        [FromServices] ApplicationDbContext dbContext,
        [FromBody, Required] BulkTrainingDataRequest request,
        CancellationToken cancellationToken)
    {
        var years = request.Years?.ToArray() ?? Array.Empty<int>();

        if (years.Length == 0)
        {
            return Results.BadRequest("At least one year must be provided.");
        }

        var psgcCodes = await dbContext.AdministrativeAreas
            .AsNoTracking()
            .Where(area => area.GeographicLevel.ToLower() == "bgy")
            .Select(area => area.PsgcCode)
            .ToListAsync(cancellationToken);

        var results = await Task.WhenAll(psgcCodes
            .Select(psgcCode => repository.GetWeeklySnapshotsAsync(
                psgcCode,
                years,
                dengueWeekNumber: null,
                dengueWeekRange: (1, 53),
                cancellationToken)));

        var snapshots = results
            .SelectMany(result => result.Snapshots)
            .OrderBy(snapshot => snapshot.PsgcCode)
            .ThenBy(snapshot => snapshot.DengueYear)
            .ThenBy(snapshot => snapshot.DengueWeekNumber)
            .ToList();

        var unprocessed = results
            .SelectMany(result => result.MissingLagWeeks)
            .Distinct()
            .OrderBy(code => code)
            .ToList();

        var aggregatedResult = new WeeklyTrainingWeatherResult(
            snapshots,
            unprocessed);

        return Results.Ok(aggregatedResult);
    }
}

