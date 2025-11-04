using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.trainingdatapipeline.endpoints;

public class TrainAdvanceModel : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/training-data")
            .WithTags("Training Data Pipeline")
            .WithSummary("Train Advance Model");

        group.MapPost("advanced", Handler);
        return group;
    }

    private static async Task<Results<Ok<ModelInfo>, ProblemHttpResult>> Handler([FromServices] IPredictionService<AdvDengueForecastInput, DengueForecastOutput> _dengue)
    {
        try
        {
            await _dengue.TrainModelAsync();
            var res = _dengue.GetModelInfo();
            return TypedResults.Ok(res);
        }
        catch (Exception e)
        {
            return TypedResults.Problem(e.Message, e.Source, 500, "Train Advance Model");
        }
    }
}