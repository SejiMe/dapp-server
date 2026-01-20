using Microsoft.AspNetCore.Http.HttpResults;
using dengue.watch.api.infrastructure.ml;

namespace dengue.watch.api.features.denguecases.commands;

public class TriggerAdvancePredictionEndpoint : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithSummary("Trigger advance prediction coordinator for a PSGC")
            .WithTags("Dengue Cases");

        group.MapPost("advance/trigger", Handler)
            .Produces<Results<Ok<TriggerPredictionResponse>, BadRequest<string>, ProblemHttpResult>>();

        return group;
    }

    public record TriggerRequest(string PsgcCode, int AggregatedYear, int AggregatedWeek);
    
    public record TriggerPredictionResponse(
        bool IsSuccess,
        List<PredictionResultRecord> Results,
        List<PredictionErrorRecord> Errors,
        string Message);

    private static async Task<Results<Ok<TriggerPredictionResponse>, BadRequest<string>, ProblemHttpResult>> Handler(
        [FromBody] TriggerRequest request,
        [FromServices] IPredictionCoordinator coordinator,
        CancellationToken cancellation = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.PsgcCode))
                return TypedResults.BadRequest("PsgcCode is required");

            if (request.AggregatedWeek < 1 || request.AggregatedWeek > 53)
                return TypedResults.BadRequest("AggregatedWeek must be between 1 and 53");

            var result = await coordinator.RunForPsgcAsync(request.PsgcCode, request.AggregatedYear, request.AggregatedWeek, cancellation);
            
            var message = result.IsSuccess 
                ? $"Successfully processed {result.Results.Count} predictions for {request.PsgcCode}"
                : $"Processing completed with {result.Errors.Count} error(s) for {request.PsgcCode}";

            var response = new TriggerPredictionResponse(
                result.IsSuccess,
                result.Results,
                result.Errors,
                message);
            
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
