using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using dengue.watch.api.features.advisories.dtos;
using dengue.watch.api.infrastructure.database;

namespace dengue.watch.api.features.advisories.queries;

/// <summary>
/// Get all advisories without pagination
/// </summary>
public class GetAll : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("advisories");

        group.MapGet("all", Handler)
            .WithName("GetAllAdvisories")
            .WithSummary("Get All Community Advisories")
            .Produces<List<CommunityAdvisoryResponse>>(200);

        return group;
    }

    public static async Task<Ok<List<CommunityAdvisoryResponse>>> Handler(
        [FromServices] ApplicationDbContext _db,
        CancellationToken cancellationToken = default)
    {
        var advisories = await _db.CommunityPreventiveAdvisories
            .OrderBy(a => a.RiskLevel)
            .ThenBy(a => a.Title)
            .ThenByDescending(a => a.IsActive)
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

        return TypedResults.Ok(advisories);
    }
}
