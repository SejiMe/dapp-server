using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace dengue.watch.api.features.denguecases.queries;

public class GetMonthlyDengueCasesByPsgcAndYear : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithTags("Dengue Cases")
            .WithSummary("Get monthly dengue case counts by PSGC and year (cached 5 minutes)");

        group.MapGet("monthly/{psgccode}", HandlerAsync)
            .WithName("GetMonthlyDengueCasesByPsgcAndYear")
            .Produces<Ok<Response>>()
            .Produces<BadRequest<string>>()
            .Produces<NotFound<string>>();

        return group;
    }

    public record Query(int Year);

    public record MonthlyCount(int Month, int CaseCount);

    public record Response(
        string PsgcCode,
        int Year,
        List<MonthlyCount> MonthlyCases);

    private static async Task<Results<Ok<Response>, BadRequest<string>, NotFound<string>>> HandlerAsync(
        string psgccode,
        [AsParameters] Query query,
        [FromServices] ApplicationDbContext db,
        [FromServices] IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(psgccode) || psgccode.Length != 10)
            return TypedResults.BadRequest("psgccode must be exactly 10 characters.");

        if (query.Year < 2012)
            return TypedResults.BadRequest("Year must be 2012 or later.");

        var exists = await db.AdministrativeAreas
            .AsNoTracking()
            .AnyAsync(a => a.PsgcCode == psgccode, cancellationToken);

        if (!exists)
            return TypedResults.NotFound($"No administrative area found for {psgccode}.");

        var cacheKey = $"dengue:monthly:{psgccode}:{query.Year}";

        if (cache.TryGetValue<Response>(cacheKey, out var cached) && cached is not null)
        {
            return TypedResults.Ok(cached);
        }

        // Derive monthly counts from weekly dengue cases using ISO week->date mapping.
        // This keeps the endpoint consistent even if MonthlyDengueCases is not populated.
        var weekly = await db.WeeklyDengueCases
            .AsNoTracking()
            .Where(w => w.PsgcCode == psgccode && w.Year == query.Year)
            .Select(w => new { w.Year, w.WeekNumber, w.CaseCount })
            .ToListAsync(cancellationToken);

        var monthLookup = weekly
            .GroupBy(w => ISOWeek.ToDateTime(w.Year, w.WeekNumber, DayOfWeek.Monday).Month)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.CaseCount));

        var monthlyCases = Enumerable.Range(1, 12)
            .Select(month => new MonthlyCount(month, monthLookup.GetValueOrDefault(month, 0)))
            .ToList();

        var response = new Response(
            PsgcCode: psgccode,
            Year: query.Year,
            MonthlyCases: monthlyCases);

        cache.Set(cacheKey, response, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return TypedResults.Ok(response);
    }
}
