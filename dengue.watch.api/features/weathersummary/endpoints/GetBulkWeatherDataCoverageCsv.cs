using System.Text;
using dengue.watch.api.features.weathersummary.services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.weathersummary.endpoints;

public class GetBulkWeatherDataCoverageCsv : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("weather-summary")
            .WithTags("Weather Summary");

        group.MapGet("coverage/bulk/csv", HandlerAsync)
            .WithName("GetBulkWeatherDataCoverageCsv")
            .WithSummary("Export weather data coverage for all barangays as CSV")
            .WithDescription("Returns a CSV file containing weather data coverage statistics for all barangays. Useful for data analysis and reporting.")
            .Produces<FileContentHttpResult>(contentType: "text/csv")
            .Produces<BadRequest<string>>();
        
        return group;
    }

    public record BulkWeatherDataCoverageCsvRequest(
        DateOnly StartDate,
        DateOnly EndDate,
        bool IncludeMissingDates = false);

    private static async Task<Results<FileContentHttpResult, BadRequest<string>>> HandlerAsync(
        [AsParameters] BulkWeatherDataCoverageCsvRequest request,
        [FromServices] IWeatherDataCoverageService coverageService,
        CancellationToken cancellationToken)
    {
        if (request.StartDate > request.EndDate)
        {
            return TypedResults.BadRequest("Start date must be less than or equal to end date.");
        }

        try
        {
            var result = await coverageService.GetBulkCoverageAsync(
                request.StartDate,
                request.EndDate,
                cancellationToken);

            var csv = GenerateCsv(result, request.IncludeMissingDates);
            var bytes = Encoding.UTF8.GetBytes(csv);
            var fileName = $"weather-coverage_{request.StartDate:yyyy-MM-dd}_to_{request.EndDate:yyyy-MM-dd}.csv";

            return TypedResults.File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"Error generating CSV: {ex.Message}");
        }
    }

    private static string GenerateCsv(BulkWeatherDataCoverageResult result, bool includeMissingDates)
    {
        var sb = new StringBuilder();

        // Summary header
        sb.AppendLine("# Weather Data Coverage Report");
        sb.AppendLine($"# Date Range: {result.StartDate:yyyy-MM-dd} to {result.EndDate:yyyy-MM-dd}");
        sb.AppendLine($"# Total Barangays: {result.TotalBarangays}");
        sb.AppendLine($"# Full Coverage (100%): {result.BarangaysWithFullCoverage}");
        sb.AppendLine($"# Partial Coverage: {result.BarangaysWithPartialCoverage}");
        sb.AppendLine($"# No Coverage (0%): {result.BarangaysWithNoCoverage}");
        sb.AppendLine($"# Average Coverage: {result.AverageCoveragePercentage}%");
        sb.AppendLine();

        // CSV header
        if (includeMissingDates)
        {
            sb.AppendLine("PsgcCode,StartDate,EndDate,TotalExpectedDays,AvailableDays,MissingDays,CoveragePercentage,MissingDates");
        }
        else
        {
            sb.AppendLine("PsgcCode,StartDate,EndDate,TotalExpectedDays,AvailableDays,MissingDays,CoveragePercentage");
        }

        // Data rows
        foreach (var item in result.Results.OrderBy(r => r.PsgcCode))
        {
            if (includeMissingDates)
            {
                var missingDatesStr = item.MissingDates.Count > 0
                    ? $"\"{string.Join(";", item.MissingDates.Select(d => d.ToString("yyyy-MM-dd")))}\""
                    : "";
                
                sb.AppendLine($"{item.PsgcCode},{item.StartDate:yyyy-MM-dd},{item.EndDate:yyyy-MM-dd},{item.TotalExpectedDays},{item.AvailableDays},{item.MissingDays},{item.CoveragePercentage},{missingDatesStr}");
            }
            else
            {
                sb.AppendLine($"{item.PsgcCode},{item.StartDate:yyyy-MM-dd},{item.EndDate:yyyy-MM-dd},{item.TotalExpectedDays},{item.AvailableDays},{item.MissingDays},{item.CoveragePercentage}");
            }
        }

        return sb.ToString();
    }
}
