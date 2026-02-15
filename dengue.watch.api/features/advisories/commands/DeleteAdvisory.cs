using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace dengue.watch.api.features.advisories.commands;

/// <summary>
/// Delete a community advisory
/// </summary>
public class DeleteAdvisory : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("advisories")
            .WithSummary("Delete Community Advisory")
            .WithTags("Community Advisories - Admin");

        group.MapDelete("{id}", Handler)
            .Produces(204)
            .Produces(404);

        return group;
    }

    private static async Task<Results<NoContent, NotFound<string>, ProblemHttpResult>> Handler(
        Guid id,
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

            _db.CommunityPreventiveAdvisories.Remove(advisory);
            await _db.SaveChangesAsync(cancellationToken);

            return TypedResults.NoContent();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem($"Failed to delete advisory: {ex.Message}");
        }
    }
}
