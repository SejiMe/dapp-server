using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.trainingdatapipeline.endpoints;

public class TrainModel : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/training-data")
            .WithTags("Training Data Pipeline")
            .WithSummary("Training Data Pipeline");

        group.MapPost("", Handler);
        return group;
    }

    private static async Task<Results<Ok<ModelInfo>, BadRequest>> Handler([FromServices] IPredictionService<DengueForecastInput, DengueForecastOutput> _dengue)
    {
        try
        {
            await _dengue.TrainModelAsync();
            var res = _dengue.GetModelInfo();
            return TypedResults.Ok(res);
        }
        catch (Exception e)
        {
            return TypedResults.BadRequest();
        }
    }
}