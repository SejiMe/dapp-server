using dengue.watch.api.infrastructure.ml;
using dengue.watch.api.common.repositories;
using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.trainingdatapipeline.endpoints;

public class ManualTriggerAdvancePrediction : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("training-data")
            .WithTags("Training Data Pipeline")
            .WithSummary("Manual trigger for advance prediction coordinator");

        group.MapPost("/advance/manual-trigger", Handler)
            .Produces<Results<Ok<PredictionCoordinatorResult>, BadRequest<string>, ProblemHttpResult>>();

        return group;
    }

    private static async Task<Results<Ok<PredictionCoordinatorResult>, BadRequest<string>, ProblemHttpResult>> Handler([
        FromBody] ManualTriggerRequest request, [FromServices] IPredictionCoordinator coordinator, CancellationToken cancellation = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.PsgcCode))
                return TypedResults.BadRequest("PsgcCode is required");

            var res = await coordinator.RunForPsgcAsync(request.PsgcCode, request.AggregatedYear, request.AggregatedWeek, cancellation);
            return TypedResults.Ok(res);
        }
        catch (ValidationException ve)
        {
            return TypedResults.BadRequest(ve.Message);
        }
        catch (Exception e)
        {
            return TypedResults.Problem(e.Message);
        }
    }

    public record ManualTriggerRequest(string PsgcCode, int AggregatedYear, int AggregatedWeek);
}
