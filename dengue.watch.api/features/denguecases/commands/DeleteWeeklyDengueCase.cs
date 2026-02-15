using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace dengue.watch.api.features.denguecases.commands;

/// <summary>
/// Delete a weekly dengue case
/// </summary>
public class DeleteWeeklyDengueCase : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithSummary("Delete Weekly Dengue Case")
            .WithTags("Dengue Cases - Admin");

        group.MapDelete("weekly/{id}", Handler)
            .Produces(204)
            .Produces(404);

        return group;
    }

    private static async Task<Results<NoContent, NotFound<string>, ProblemHttpResult>> Handler(
        long id,
        [FromServices] ApplicationDbContext _db,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var weeklyCase = await _db.WeeklyDengueCases
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

            if (weeklyCase == null)
            {
                return TypedResults.NotFound($"Weekly dengue case with ID '{id}' not found");
            }

            _db.WeeklyDengueCases.Remove(weeklyCase);
            await _db.SaveChangesAsync(cancellationToken);

            return TypedResults.NoContent();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem($"Failed to delete weekly dengue case: {ex.Message}");
        }
    }
}
