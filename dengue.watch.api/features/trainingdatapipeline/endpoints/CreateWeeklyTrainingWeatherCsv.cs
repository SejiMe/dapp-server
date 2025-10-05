using System.ComponentModel.DataAnnotations;
using dengue.watch.api.features.trainingdatapipeline.models;
using dengue.watch.api.features.trainingdatapipeline.repositories;
using dengue.watch.api.features.trainingdatapipeline.services;
using Microsoft.AspNetCore.Mvc;

namespace dengue.watch.api.features.trainingdatapipeline.endpoints;

public sealed class CreateWeeklyTrainingWeatherCsv : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/training-data")
            .WithTags("Training Data Pipeline")
            .WithOpenApi();

        group.MapPost("/weekly-weather/csv", HandleAsync)
            .WithName("CreateWeeklyTrainingWeatherCsv")
            .WithSummary("Generate weekly training weather CSV file")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> HandleAsync(
        [FromServices] IWeeklyTrainingWeatherRepository repository,
        [FromServices] ITrainingDataCsvService csvService,
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

        var weekRange = request.WeekRange is null
    ? (1, 52)
    : (request.WeekRange.From, request.WeekRange.To);

        var result = await repository.GetWeeklySnapshotsAsync(
            request.PsgcCode,
            years,
            request.WeekNumber,
            weekRange,
            cancellationToken);

        var csvFile = csvService.CreateCsv(result, request.isPsgcExcludedInResult
        );
    
        return Results.File(
            csvFile.Content,
            csvFile.ContentType,
            csvFile.FileName,
            enableRangeProcessing: false,
            lastModified: DateTimeOffset.UtcNow,
            entityTag: null);
    }
}
