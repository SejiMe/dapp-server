using dengue.watch.api.features.weathersummary.services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.weathersummary.endpoints;

public class GetBulkWeatherDataCoverage : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("weather-summary")
            .WithTags("Weather Summary");

        group.MapGet("coverage/bulk", HandlerAsync)
            .WithName("GetBulkWeatherDataCoverage")
            .WithSummary("Verify weather data coverage for all barangays within a date range")
            .WithDescription("Returns weather data coverage statistics for all barangays with coordinates. Use this to identify gaps in weather data collection.")
            .Produces<Ok<BulkWeatherDataCoverageResult>>()
            .Produces<BadRequest<string>>();
        
        return group;
    }

    public record BulkWeatherDataCoverageRequest(
        DateOnly StartDate,
        DateOnly EndDate);

    private static async Task<Results<Ok<BulkWeatherDataCoverageResult>, BadRequest<string>>> HandlerAsync(
        [AsParameters] BulkWeatherDataCoverageRequest request,
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

            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"Error querying weather data: {ex.Message}");
        }
    }
}
