using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.trainingdatapipeline.endpoints;

public class GetAdvanceModelInfo : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/training-data")
            .WithTags("Training Data Pipeline")
            .WithSummary("Get Advanced Model Info");

        group.MapGet("model-info/basic", Handler);
        return group;
    }

    private static Results<Ok<ModelInfo>, ProblemHttpResult> Handler([FromServices] IPredictionService<AdvDengueForecastInput, DengueForecastOutput> _predictionService)
    {
        try
        {
            ModelInfo modelInfo =  _predictionService.GetModelInfo();
            return TypedResults.Ok(modelInfo);
        }
        catch (Exception e)
        {
            return TypedResults.Problem(e.Message);
        }
        
    }
}