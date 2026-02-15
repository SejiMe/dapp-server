using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using dengue.watch.api.features.denguecases.dtos;

namespace dengue.watch.api.features.denguecases.commands;

/// <summary>
/// Update an existing weekly dengue case
/// </summary>
public class UpdateWeeklyDengueCase : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithSummary("Update Weekly Dengue Case")
            .WithTags("Dengue Cases - Admin");

        group.MapPut("weekly/{id}", Handler)
            .Produces<WeeklyDengueCaseResponse>(200)
            .Produces(400)
            .Produces(404);

        return group;
    }

    private static async Task<Results<Ok<WeeklyDengueCaseResponse>, BadRequest<string>, NotFound<string>, ProblemHttpResult>> Handler(
        long id,
        [FromBody] UpdateWeeklyDengueCaseRequest request,
        [FromServices] ApplicationDbContext _db,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var weeklyCase = await _db.WeeklyDengueCases
                .Include(w => w.AdministrativeArea)
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

            if (weeklyCase == null)
            {
                return TypedResults.NotFound($"Weekly dengue case with ID '{id}' not found");
            }

            // Apply updates
            if (request.Year.HasValue) weeklyCase.Year = request.Year.Value;
            if (request.WeekNumber.HasValue) weeklyCase.WeekNumber = request.WeekNumber.Value;
            if (request.CaseCount.HasValue) weeklyCase.CaseCount = request.CaseCount.Value;

            // Validate no duplicate after update
            var duplicate = await _db.WeeklyDengueCases
                .FirstOrDefaultAsync(w => 
                    w.Id != id &&
                    w.PsgcCode == weeklyCase.PsgcCode && 
                    w.Year == weeklyCase.Year && 
                    w.WeekNumber == weeklyCase.WeekNumber, 
                    cancellationToken);

            if (duplicate != null)
            {
                return TypedResults.BadRequest($"Another record already exists for PSGC '{weeklyCase.PsgcCode}', Year {weeklyCase.Year}, Week {weeklyCase.WeekNumber}");
            }

            await _db.SaveChangesAsync(cancellationToken);

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
        catch (Exception ex)
        {
            return TypedResults.Problem($"Failed to update weekly dengue case: {ex.Message}");
        }
    }
}
