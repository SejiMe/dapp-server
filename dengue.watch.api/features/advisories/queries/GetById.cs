using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using dengue.watch.api.features.advisories.dtos;
using dengue.watch.api.infrastructure.database;

namespace dengue.watch.api.features.advisories.queries;

/// <summary>
/// Get a single advisory by ID
/// </summary>
public class GetById : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("advisories");

        group.MapGet("{id}", Handler)
            .WithName("GetAdvisoryById")
            .Produces<CommunityAdvisoryResponse>(200)
            .Produces(404);

        return group;
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
