using System.ComponentModel.DataAnnotations;
using dengue.watch.api.features.trainingdatapipeline.models;
using dengue.watch.api.features.trainingdatapipeline.services;

namespace dengue.watch.api.features.trainingdatapipeline.endpoints;

public class CreateWeeklyBulkTrainingCSV : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/training-data")
            .WithTags("Training Data Pipeline")
            .WithOpenApi();

        group.MapPost("/weekly-weather/all/csv", HandleAsync)
            .WithName("CreateWeeklyTrainingAllCsv")
            .WithSummary("Generate weekly training weather CSV file for all")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> HandleAsync(
        [FromServices] IAggregatedWeeklyHistoricalWeatherRepository repository,
        [FromServices] ITrainingDataCsvService csvService,
        [FromServices] ApplicationDbContext _context,
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
            ? (1, 53)
            : (request.WeekRange.From, request.WeekRange.To);
     
        var psgcCodes = await _context.AdministrativeAreas
            .AsNoTracking()
            .Where(area => area.GeographicLevel.ToLower() == "bgy")
            .Select(area => area.PsgcCode)
            .ToListAsync(cancellationToken);

        
        
        var resultsArrays = await Task.WhenAll(psgcCodes
            .Select(psgcCode => repository.GetWeeklySnapshotsAsync(
                psgcCode,
                years,
                dengueWeekNumber: null,
                dengueWeekRange: (1, 53),
                cancellationToken)));
        
        WeeklyTrainingWeatherResult results =  new (new List<WeeklyTrainingWeatherSnapshot>(), new List<string>());

        foreach (var weeklyResult in resultsArrays)
        {
            results.Snapshots.AddRange(weeklyResult.Snapshots);
            results.MissingLagWeeks.AddRange(weeklyResult.MissingLagWeeks);
        } 

        var csvFile = csvService.CreateCsv(results, request.isPsgcExcludedInResult);
    
        return Results.File(
            csvFile.Content,
            csvFile.ContentType,
            "all-weekly-training-data.csv",
            enableRangeProcessing: false,
            lastModified: DateTimeOffset.UtcNow,
            entityTag: null);
    }
}