using Microsoft.AspNetCore.Http.HttpResults;
using dengue.watch.api.infrastructure.ml;

namespace dengue.watch.api.features.trainingdatapipeline.endpoints;

public class TriggerBulkAdvancePredictionByWeek : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("training-data")
            .WithTags("Training Data Pipeline")
            .WithSummary("Trigger bulk advance predictions for all barangays by specific ISO week");

        group.MapPost("/advance/bulk-trigger-by-week", Handler)
            .Produces<Results<Ok<BulkPredictionResultResponse>, BadRequest<string>, ProblemHttpResult>>();

        return group;
    }

    public record BulkPredictionRequest(int AggregatedYear, int AggregatedWeek);
    
    public record BulkPredictionResultResponse(
        int ProcessedCount,
        int SkippedCount,
        int ErrorCount,
        int TotalBarangays,
        int AggregatedYear,
        int AggregatedWeek,
        string Summary,
        bool HasErrors,
        List<PredictionErrorRecord> Errors);

    private static async Task<Results<Ok<BulkPredictionResultResponse>, BadRequest<string>, ProblemHttpResult>> Handler(
        [FromBody] BulkPredictionRequest request,
        [FromServices] IPredictionCoordinator coordinator,
        CancellationToken cancellation = default)
    {
        try
        {
            if (request.AggregatedWeek < 1 || request.AggregatedWeek > 53)
                return TypedResults.BadRequest("AggregatedWeek must be between 1 and 53");

            if (request.AggregatedYear < 2012)
                return TypedResults.BadRequest("AggregatedYear must be 2012 or later");

            var result = await coordinator.RunForAllByWeekAsync(request.AggregatedYear, request.AggregatedWeek, cancellation);

            var response = new BulkPredictionResultResponse(
                result.ProcessedCount,
                result.SkippedCount,
                result.ErrorCount,
                result.TotalBarangays,
                request.AggregatedYear,
                request.AggregatedWeek,
                result.Summary,
                result.HasErrors,
                result.Errors
            );

            return TypedResults.Ok(response);
        }
        catch (ValidationException ve)
        {
            return TypedResults.BadRequest(ve.Message);
        }
        catch (Exception ex)
        {
            return TypedResults.Problem($"Unexpected error: {ex.Message}");
        }
    }
}
