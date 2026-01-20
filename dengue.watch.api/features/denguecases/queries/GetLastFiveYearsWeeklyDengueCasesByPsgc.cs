using dengue.watch.api.features.denguecases.dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace dengue.watch.api.features.denguecases.queries;

public class GetLastFiveYearsWeeklyDengueCasesByPsgc : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithTags("Dengue Cases")
            .WithSummary("Get weekly dengue cases for a PSGC in 5-year pages (latest years first)");

        group.MapGet("historical/{psgccode}/last-5-years", HandlerAsync)
            .WithName("GetLastFiveYearsWeeklyDengueCasesByPsgc")
            .Produces<Ok<Response>>()
            .Produces<BadRequest<string>>()
            .Produces<NotFound<string>>();

        return group;
    }

    public record Query(int Page = 1);

    public record Response(
        string PsgcCode,
        int Page,
        int PageSizeYears,
        List<int> Years,
        List<HistoricalWeeklyDengueCases> DengueCases);

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

        var years = await db.WeeklyDengueCases
            .AsNoTracking()
            .Where(w => w.PsgcCode == psgccode)
            .Select(w => w.Year)
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
                DengueCases: []));
        }

        var dengueCases = await db.WeeklyDengueCases
            .AsNoTracking()
            .Where(w => w.PsgcCode == psgccode && years.Contains(w.Year))
            .OrderBy(w => w.Year)
            .ThenBy(w => w.WeekNumber)
            .Select(w => new HistoricalWeeklyDengueCases(w.Year, w.WeekNumber, w.CaseCount))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new Response(
            PsgcCode: psgccode,
            Page: query.Page,
            PageSizeYears: 5,
            Years: years.OrderBy(y => y).ToList(),
            DengueCases: dengueCases));
    }
}
