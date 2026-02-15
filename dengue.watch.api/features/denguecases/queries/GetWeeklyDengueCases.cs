using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using dengue.watch.api.features.denguecases.dtos;

namespace dengue.watch.api.features.denguecases.queries;

/// <summary>
/// Get weekly dengue cases with pagination
/// </summary>
public class GetWeeklyDengueCases : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithSummary("Get Weekly Dengue Cases")
            .WithTags("Dengue Cases - Admin");

        group.MapGet("weekly", Handler)
            .Produces<WeeklyDengueCasesListResponse>(200)
            .Produces(400);

        group.MapGet("weekly/{id}", GetById.Handler)
            .Produces<WeeklyDengueCaseResponse>(200)
            .Produces(404);

        return group;
    }

    public record FilterParameters(
        int PageNumber = 1,
        int PageSize = 20,
        string? PsgcCode = null,
        int? Year = null
    );

    private static async Task<Ok<WeeklyDengueCasesListResponse>> Handler(
        [AsParameters] FilterParameters parameters,
        [FromServices] ApplicationDbContext _db,
        CancellationToken cancellationToken = default)
    {
        var query = _db.WeeklyDengueCases
            .Include(w => w.AdministrativeArea)
            .AsQueryable();

        // Filter by PSGC code if provided
        if (!string.IsNullOrEmpty(parameters.PsgcCode))
        {
            query = query.Where(w => w.PsgcCode == parameters.PsgcCode);
        }

        // Filter by year if provided
        if (parameters.Year.HasValue)
        {
            query = query.Where(w => w.Year == parameters.Year.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var cases = await query
            .OrderByDescending(w => w.Year)
            .ThenByDescending(w => w.WeekNumber)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(w => new WeeklyDengueCaseResponse(
                w.Id,
                w.PsgcCode,
                w.AdministrativeArea != null ? w.AdministrativeArea.Name : string.Empty,
                w.Year,
                w.WeekNumber,
                w.CaseCount
            ))
            .ToListAsync(cancellationToken);

        var response = new WeeklyDengueCasesListResponse(
            parameters.PageNumber,
            parameters.PageSize,
            totalCount,
            cases
        );

        return TypedResults.Ok(response);
    }
}

/// <summary>
/// Get a single weekly dengue case by ID
/// </summary>
public class GetById : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        return app;
    }

    public static async Task<Results<Ok<WeeklyDengueCaseResponse>, NotFound>> Handler(
        long id,
        [FromServices] ApplicationDbContext _db,
        CancellationToken cancellationToken = default)
    {
        var weeklyCase = await _db.WeeklyDengueCases
            .Include(w => w.AdministrativeArea)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (weeklyCase == null)
        {
            return TypedResults.NotFound();
        }

        var response = new WeeklyDengueCaseResponse(
            weeklyCase.Id,
            weeklyCase.PsgcCode,
            weeklyCase.AdministrativeArea?.Name ?? string.Empty,
            weeklyCase.Year,
            weeklyCase.WeekNumber,
            weeklyCase.CaseCount
        );

        return TypedResults.Ok(response);
    }
}
