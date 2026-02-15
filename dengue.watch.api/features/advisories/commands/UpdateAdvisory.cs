using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using dengue.watch.api.features.advisories.dtos;

namespace dengue.watch.api.features.advisories.commands;

/// <summary>
/// Update an existing community advisory
/// </summary>
public class UpdateAdvisory : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("advisories")
            .WithSummary("Update Community Advisory")
            .WithTags("Community Advisories - Admin");

        group.MapPut("{id}", Handler)
            .Produces<CommunityAdvisoryResponse>(200)
            .Produces(400)
            .Produces(404);

        return group;
    }

    private static async Task<Results<Ok<CommunityAdvisoryResponse>, BadRequest<string>, NotFound<string>, ProblemHttpResult>> Handler(
        Guid id,
        [FromBody] UpdateAdvisoryRequest request,
        [FromServices] ApplicationDbContext _db,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var advisory = await _db.CommunityPreventiveAdvisories
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (advisory == null)
            {
                return TypedResults.NotFound($"Advisory with ID '{id}' not found");
            }

            // Apply updates
            if (!string.IsNullOrEmpty(request.Title))
                advisory.Title = request.Title;
            if (!string.IsNullOrEmpty(request.Description))
                advisory.Description = request.Description;
            if (!string.IsNullOrEmpty(request.ActionPlan))
                advisory.ActionPlan = request.ActionPlan;
            if (request.RiskLevel.HasValue)
                advisory.RiskLevel = request.RiskLevel.Value;
            if (request.IsActive.HasValue)
                advisory.IsActive = request.IsActive.Value;

            advisory.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

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
        catch (Exception ex)
        {
            return TypedResults.Problem($"Failed to update advisory: {ex.Message}");
        }
    }
}
