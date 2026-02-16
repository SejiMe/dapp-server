using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace dengue.watch.api.features.denguecases.queries;

public class GetMonthlyAveragePredictedDengueCasesByPsgcAndYear : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithSummary("Get Monthly Average Predicted Dengue Cases by PSGC Code and ISO Year")
            .WithTags("Dengue Cases - Statistics");

        group.MapGet("{psgccode}/monthly-average/{year}", Handler)
            .Produces<Ok<MonthlyAverageResponse>>()
            .Produces<BadRequest<string>>()
            .Produces<NotFound<string>>();

        return group;
    }

    public record MonthlyData(
        int Month,
        string MonthName,
        double AveragePredictedCases,
        int TotalWeeks,
        double MinPredictedCases,
        double MaxPredictedCases
    );

    public record MonthlyAverageResponse(
        string PsgcCode,
        string BarangayName,
        int Year,
        List<MonthlyData> MonthlyData,
        double YearlyAverage
    );

    private static async Task<Results<Ok<MonthlyAverageResponse>, BadRequest<string>, NotFound<string>, ProblemHttpResult>> Handler(
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

            // Group by month and calculate statistics
            var monthlyData = predictedCases
                .GroupBy(p => p.PredictedIsoWeek switch
                {
                    >= 1 and <= 4 => 1,  // January
                    >= 5 and <= 8 => 2,  // February
                    >= 9 and <= 13 => 3, // March
                    >= 14 and <= 17 => 4, // April
                    >= 18 and <= 22 => 5, // May
                    >= 23 and <= 26 => 6, // June
                    >= 27 and <= 30 => 7, // July
                    >= 31 and <= 35 => 8, // August
                    >= 36 and <= 39 => 9, // September
                    >= 40 and <= 43 => 10, // October
                    >= 44 and <= 48 => 11, // November
                    _ => 12 // December
                })
                .Select(g => new MonthlyData(
                    g.Key,
                    GetMonthName(g.Key),
                    Math.Round(g.Average(p => p.PredictedValue), 2),
                    g.Count(),
                    g.Min(p => p.PredictedValue),
                    g.Max(p => p.PredictedValue)
                ))
                .OrderBy(m => m.Month)
                .ToList();

            // Calculate yearly average
            var yearlyAverage = Math.Round(predictedCases.Average(p => p.PredictedValue), 2);

            var response = new MonthlyAverageResponse(
                psgccode,
                barangay.Name,
                year,
                monthlyData,
                yearlyAverage
            );

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            return TypedResults.Problem($"Failed to get monthly average predicted cases: {ex.Message}");
        }
    }

    private static string GetMonthName(int month)
    {
        return month switch
        {
            1 => "January",
            2 => "February",
            3 => "March",
            4 => "April",
            5 => "May",
            6 => "June",
            7 => "July",
            8 => "August",
            9 => "September",
            10 => "October",
            11 => "November",
            12 => "December",
            _ => "Unknown"
        };
    }
}