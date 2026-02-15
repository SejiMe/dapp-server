using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using dengue.watch.api.features.advisories.dtos;
using dengue.watch.api.infrastructure.database;

namespace dengue.watch.api.features.advisories.queries;

/// <summary>
/// Get community advisories with pagination and filtering
/// </summary>
public class GetAdvisories : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("advisories")
            .WithSummary("Get Community Advisories")
            .WithTags("Community Advisories");

        group.MapGet("", Handler)
            .Produces<AdvisoriesListResponse>(200);

        group.MapGet("{id}", GetById.Handler)
            .Produces<CommunityAdvisoryResponse>(200)
            .Produces(404);

        return group;
    }

    public record FilterParameters(
        int PageNumber = 1,
        int PageSize = 20,
        string? RiskLevel = null,
        bool? IsActive = null
    );

    private static async Task<Ok<AdvisoriesListResponse>> Handler(
        [AsParameters] FilterParameters parameters,
        [FromServices] ApplicationDbContext _db,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CommunityPreventiveAdvisories.AsQueryable();

        // Filter by risk level
        if (!string.IsNullOrEmpty(parameters.RiskLevel))
        {
            if (Enum.TryParse<RiskLevel>(parameters.RiskLevel, true, out var riskLevel))
            {
                query = query.Where(a => a.RiskLevel == riskLevel);
            }
        }

        // Filter by active status
        if (parameters.IsActive.HasValue)
        {
            query = query.Where(a => a.IsActive == parameters.IsActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var advisories = await query
            .OrderByDescending(a => a.RiskLevel)
            .ThenByDescending(a => a.CreatedAt)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(a => new CommunityAdvisoryResponse(
                a.Id,
                a.Title,
                a.Description,
                a.ActionPlan,
                a.RiskLevel.ToString(),
                a.CreatedAt,
                a.UpdatedAt,
                a.CreatedBy,
                a.IsActive
            ))
            .ToListAsync(cancellationToken);

        var response = new AdvisoriesListResponse(
            parameters.PageNumber,
            parameters.PageSize,
            totalCount,
            advisories
        );

        return TypedResults.Ok(response);
    }
}

/// <summary>
/// Get a single advisory by ID
/// </summary>
public class GetById : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        return app;
    }

    public static async Task<Results<Ok<CommunityAdvisoryResponse>, NotFound>> Handler(
        Guid id,
        [FromServices] ApplicationDbContext _db,
        CancellationToken cancellationToken = default)
    {
        var advisory = await _db.CommunityPreventiveAdvisories
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (advisory == null)
        {
            return TypedResults.NotFound();
        }

        var response = new CommunityAdvisoryResponse(
            advisory.Id,
            advisory.Title,
            advisory.Description,
            advisory.ActionPlan,
            advisory.RiskLevel.ToString(),
            advisory.CreatedAt,
            advisory.UpdatedAt,
            advisory.CreatedBy,
            advisory.IsActive
        );

        return TypedResults.Ok(response);
    }
}
