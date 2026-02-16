using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace dengue.watch.api.features.denguecases.queries;

public class GetYearlyAveragePredictedDengueCasesByPsgcAndYear : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithSummary("Get Yearly Average Predicted Dengue Cases by PSGC Code and ISO Year")
            .WithTags("Dengue Cases - Statistics");

        group.MapGet("{psgccode}/yearly-average/{year}", Handler)
            .Produces<Ok<YearlyAverageResponse>>()
            .Produces<BadRequest<string>>()
            .Produces<NotFound<string>>();

        return group;
    }

    public record YearlyAverageResponse(
        string PsgcCode,
        string BarangayName,
        int Year,
        double AveragePredictedCases,
        int TotalWeeks,
        double MinPredictedCases,
        double MaxPredictedCases,
        double AveragePredictedOutbreakProbability
    );

    private static async Task<Results<Ok<YearlyAverageResponse>, BadRequest<string>, NotFound<string>, ProblemHttpResult>> Handler(
        string psgccode,
        int year,
        [FromServices] ApplicationDbContext _db,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate year
            if (year < 2020 || year > DateTime.UtcNow.Year + 1)
            {
                return TypedResults.BadRequest("Year must be between 2020 and " + (DateTime.UtcNow.Year + 1));
            }

            // Check if barangay exists
            var barangay = await _db.AdministrativeAreas
                .FirstOrDefaultAsync(p => p.PsgcCode == psgccode, cancellationToken);

            if (barangay is null)
                return TypedResults.NotFound("Barangay not found");

            // Get all predicted cases for the specified year and psgccode
            var predictedCases = await _db.PredictedWeeklyDengues
                .Where(p => p.PsgcCode == psgccode && p.PredictedIsoYear == year)
                .ToListAsync(cancellationToken);

            if (!predictedCases.Any())
                return TypedResults.NotFound("No predicted cases found for the specified year");

            // Calculate statistics
            var totalCases = predictedCases.Sum(p => p.PredictedValue);
            var totalWeeks = predictedCases.Count;
            var averageCases = totalWeeks > 0 ? (double)totalCases / totalWeeks : 0;
            var minCases = predictedCases.Min(p => p.PredictedValue);
            var maxCases = predictedCases.Max(p => p.PredictedValue);
            var averageOutbreakProbability = totalWeeks > 0 ? predictedCases.Average(p => p.ProbabilityOfOutbreak) : 0;

            var response = new YearlyAverageResponse(
                psgccode,
                barangay.Name,
                year,
                Math.Round(averageCases, 2),
                totalWeeks,
                minCases,
                maxCases,
                Math.Round(averageOutbreakProbability, 4)
            );

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            return TypedResults.Problem($"Failed to get yearly average predicted cases: {ex.Message}");
        }
    }
}