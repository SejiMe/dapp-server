using dengue.watch.api.features.weathersummary.services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.weathersummary.endpoints;

public class GetWeatherDataCoverage : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("weather-summary")
            .WithTags("Weather Summary");

        group.MapGet("coverage", HandlerAsync)
            .WithName("GetWeatherDataCoverage")
            .WithSummary("Verify weather data coverage by date range and PSGC code")
            .WithDescription("Returns a list of dates with available weather data and a list of missing dates within the specified range.")
            .Produces<Ok<WeatherDataCoverageResult>>()
            .Produces<BadRequest<string>>();
        
        return group;
    }

    public record WeatherDataCoverageRequest(
        string PsgcCode,
        DateOnly StartDate,
        DateOnly EndDate);

    private static async Task<Results<Ok<WeatherDataCoverageResult>, BadRequest<string>>> HandlerAsync(
        [AsParameters] WeatherDataCoverageRequest request,
        [FromServices] IWeatherDataCoverageService coverageService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PsgcCode) || request.PsgcCode.Length != 10)
        {
            return TypedResults.BadRequest("PSGC code must be exactly 10 characters.");
        }

        if (request.StartDate > request.EndDate)
        {
            return TypedResults.BadRequest("Start date must be less than or equal to end date.");
        }

        var maxRangeDays = 366;
        var totalDays = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        if (totalDays > maxRangeDays)
        {
            return TypedResults.BadRequest($"Date range cannot exceed {maxRangeDays} days.");
        }

        try
        {
            var result = await coverageService.GetCoverageAsync(
                request.PsgcCode,
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
