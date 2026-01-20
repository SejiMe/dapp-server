using Microsoft.AspNetCore.Http.HttpResults;
using dengue.watch.api.infrastructure.database;

namespace dengue.watch.api.features.denguecases.queries;

public class GetPredictedByIsoWeek : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithSummary("Get predicted dengue value by PSGC and ISO week")
            .WithTags("Dengue Cases");

        group.MapGet("predicted/{psgccode}/{year}/{week}", Handler)
            .Produces<Results<Ok<PredictedWeeklyDengueResponse>, NotFound<string>, BadRequest<string>, ProblemHttpResult>>();

        return group;
    }

    public record PredictedWeeklyDengueResponse(
        Guid PredictionId,
        string PsgcCode,
        int PredictedIsoYear,
        int PredictedIsoWeek,
        int PredictedValue,
        double ProbabilityOfOutbreak,
        float LowerBound,
        float UpperBound,
        string RiskLevel,
        string MonthName
    );

    private static async Task<Results<Ok<PredictedWeeklyDengueResponse>, NotFound<string>, BadRequest<string>, ProblemHttpResult>> Handler(
        string psgccode,
        int year,
        int week,
        [FromServices] ApplicationDbContext db,
        CancellationToken cancellation = default)
    {
        try
        {
            if (week < 1 || week > 53)
                return TypedResults.BadRequest("Week must be between 1 and 53");

            var pred = await db.PredictedWeeklyDengues
                .Where(p => p.PsgcCode == psgccode && p.PredictedIsoYear == year && p.PredictedIsoWeek == week)
                .Select(p => new PredictedWeeklyDengueResponse(
                    p.PredictionId,
                    p.PsgcCode,
                    p.PredictedIsoYear,
                    p.PredictedIsoWeek,
                    p.PredictedValue,
                    p.ProbabilityOfOutbreak,
                    p.LowerBound,
                    p.UpperBound,
                    p.RiskLevel ?? string.Empty,
                    p.MonthName ?? string.Empty
                ))
                .FirstOrDefaultAsync(cancellation);

            if (pred == null)
                return TypedResults.NotFound("Prediction not found for provided PSGC and ISO week");

            return TypedResults.Ok(pred);
        }
        catch (Exception e)
        {
            return TypedResults.Problem(e.Message);
        }
    }
}
