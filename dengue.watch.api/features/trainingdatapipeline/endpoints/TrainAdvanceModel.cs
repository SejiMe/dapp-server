using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.trainingdatapipeline.endpoints;

public class TrainAdvanceModel : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("training-data")
            .WithTags("Training Data Pipeline")
            .WithSummary("Train Advance Model");

        group.MapPost("advanced", Handler);
        return group;
    }

    private static async Task<Results<Accepted<string>, ProblemHttpResult>> Handler([FromServices] ITrainingQueue queue)
    {
        try
        {
            var opId = queue.EnqueueTraining();
            // Return 202 with operation id
            return TypedResults.Accepted($"/training-data/operations/{opId}", opId);
        }
        catch (Exception e)
        {
            return TypedResults.Problem(e.Message, e.Source, 500, "Train Advance Model");
        }
    }
}