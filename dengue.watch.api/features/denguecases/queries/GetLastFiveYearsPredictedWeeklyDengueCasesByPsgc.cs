using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace dengue.watch.api.features.denguecases.queries;

public class GetLastFiveYearsPredictedWeeklyDengueCasesByPsgc : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithTags("Dengue Cases")
            .WithSummary("Get predicted weekly dengue cases for a PSGC in 5-year pages (latest years first)");

        group.MapGet("predicted/{psgccode}/last-5-years", HandlerAsync)
            .WithName("GetLastFiveYearsPredictedWeeklyDengueCasesByPsgc")
            .Produces<Ok<Response>>()
            .Produces<BadRequest<string>>()
            .Produces<NotFound<string>>();

        return group;
    }

    public record Query(int Page = 1);

    public record PredictedWeeklyDengueCaseDto(
        Guid PredictionId,
        string PsgcCode,
        int LaggedIsoYear,
        int LaggedIsoWeek,
        int PredictedIsoYear,
        int PredictedIsoWeek,
        int PredictedValue,
        float LowerBound,
        float UpperBound,
        double ConfidencePercentage,
        double ProbabilityOfOutbreak,
        string RiskLevel,
        string? MonthName);

    public record Response(
        string PsgcCode,
        int Page,
        int PageSizeYears,
        List<int> Years,
        List<PredictedYearGroup> Values);

    public record PredictedYearGroup(
        int IsoYear,
        List<PredictedWeeklyDengueCaseDto> Values);

    private static async Task<Results<Ok<Response>, BadRequest<string>, NotFound<string>>> HandlerAsync(
        string psgccode,
        [AsParameters] Query query,
        [FromServices] ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(psgccode) || psgccode.Length != 10)
            return TypedResults.BadRequest("psgccode must be exactly 10 characters.");

        if (query.Page < 1)
            return TypedResults.BadRequest("Page must be >= 1.");

        var exists = await db.AdministrativeAreas
            .AsNoTracking()
            .AnyAsync(a => a.PsgcCode == psgccode, cancellationToken);

        if (!exists)
            return TypedResults.NotFound($"No administrative area found for {psgccode}.");

        // Page over distinct predicted years (latest first)
        var years = await db.PredictedWeeklyDengues
            .AsNoTracking()
            .Where(p => p.PsgcCode == psgccode)
            .Select(p => p.PredictedIsoYear)
            .Distinct()
            .OrderByDescending(y => y)
            .Skip((query.Page - 1) * 5)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (years.Count == 0)
        {
            return TypedResults.Ok(new Response(
                PsgcCode: psgccode,
                Page: query.Page,
                PageSizeYears: 5,
                Years: [],
                Values: []));
        }

        var predictedCases = await db.PredictedWeeklyDengues
            .AsNoTracking()
            .Where(p => p.PsgcCode == psgccode && years.Contains(p.PredictedIsoYear))
            .OrderBy(p => p.PredictedIsoYear)
            .ThenBy(p => p.PredictedIsoWeek)
            .Select(p => new PredictedWeeklyDengueCaseDto(
                p.PredictionId,
                p.PsgcCode,
                p.LaggedIsoYear,
                p.LaggedIsoWeek,
                p.PredictedIsoYear,
                p.PredictedIsoWeek,
                p.PredictedValue,
                p.LowerBound,
                p.UpperBound,
                p.ConfidencePercentage,
                p.ProbabilityOfOutbreak,
                p.RiskLevel,
                p.MonthName))
            .ToListAsync(cancellationToken);

        var groupedByYear = predictedCases
            .GroupBy(p => p.PredictedIsoYear)
            .OrderByDescending(g => g.Key)
            .Select(g => new PredictedYearGroup(
                IsoYear: g.Key,
                Values: g.ToList()))
            .ToList();

        return TypedResults.Ok(new Response(
            PsgcCode: psgccode,
            Page: query.Page,
            PageSizeYears: 5,
            Years: years.OrderBy(y => y).ToList(),
            Values: groupedByYear));
    }
}
