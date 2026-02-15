using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using dengue.watch.api.features.denguecases.dtos;

namespace dengue.watch.api.features.denguecases.commands;

/// <summary>
/// Create a new weekly dengue case
/// </summary>
public class CreateWeeklyDengueCase : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithSummary("Create Weekly Dengue Case")
            .WithTags("Dengue Cases - Admin");

        group.MapPost("weekly", Handler)
            .Produces<WeeklyDengueCaseResponse>(201)
            .Produces(400)
            .Produces(404);

        return group;
    }

    private static async Task<Results<Created<WeeklyDengueCaseResponse>, BadRequest<string>, NotFound<string>, ProblemHttpResult>> Handler(
        [FromBody] CreateWeeklyDengueCaseRequest request,
        [FromServices] ApplicationDbContext _db,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate barangay exists
            var barangay = await _db.AdministrativeAreas
                .FirstOrDefaultAsync(a => a.PsgcCode == request.PsgcCode, cancellationToken);

            if (barangay == null)
            {
                return TypedResults.NotFound($"Barangay with PSGC code '{request.PsgcCode}' not found");
            }

            // Check if record already exists
            var existingRecord = await _db.WeeklyDengueCases
                .FirstOrDefaultAsync(w => 
                    w.PsgcCode == request.PsgcCode && 
                    w.Year == request.Year && 
                    w.WeekNumber == request.WeekNumber, 
                    cancellationToken);

            if (existingRecord != null)
            {
                return TypedResults.BadRequest($"Record already exists for PSGC '{request.PsgcCode}', Year {request.Year}, Week {request.WeekNumber}");
            }

            var weeklyCase = new WeeklyDengueCase
            {
                PsgcCode = request.PsgcCode,
                Year = request.Year,
                WeekNumber = request.WeekNumber,
                CaseCount = request.CaseCount
            };

            await _db.WeeklyDengueCases.AddAsync(weeklyCase, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            var response = new WeeklyDengueCaseResponse(
                weeklyCase.Id,
                weeklyCase.PsgcCode,
                barangay.Name,
                weeklyCase.Year,
                weeklyCase.WeekNumber,
                weeklyCase.CaseCount
            );

            return TypedResults.Created($"/api/dengue-cases/weekly/{weeklyCase.Id}", response);
        }
        catch (Exception ex)
        {
            return TypedResults.Problem($"Failed to create weekly dengue case: {ex.Message}");
        }
    }
}
