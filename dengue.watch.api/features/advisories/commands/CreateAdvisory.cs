using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using dengue.watch.api.features.advisories.dtos;
using dengue.watch.api.infrastructure.database;

namespace dengue.watch.api.features.advisories.commands;

/// <summary>
/// Create a new community advisory
/// </summary>
public class CreateAdvisory : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("advisories")
            .WithSummary("Create Community Advisory")
            .WithTags("Community Advisories - Admin");

        group.MapPost("", Handler)
            .Produces<CommunityAdvisoryResponse>(201)
            .Produces(400);

        return group;
    }

    private static async Task<Results<Created<CommunityAdvisoryResponse>, BadRequest<string>, ProblemHttpResult>> Handler(
        [FromBody] CreateAdvisoryRequest request,
        [FromServices] ApplicationDbContext _db,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var advisory = new CommunityPreventiveAdvisory
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                ActionPlan = request.ActionPlan,
                RiskLevel = request.RiskLevel,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "admin",
                IsActive = true
            };

            await _db.CommunityPreventiveAdvisories.AddAsync(advisory, cancellationToken);
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

            return TypedResults.Created($"/api/advisories/{advisory.Id}", response);
        }
        catch (Exception ex)
        {
            return TypedResults.Problem($"Failed to create advisory: {ex.Message}");
        }
    }
}
