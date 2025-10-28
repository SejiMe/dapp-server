using dengue.watch.api.features.weathersummary.models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.weathersummary.endpoints;

public class GetCurrentDateLagged2WeekSummary : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/weather-summary")
            .WithName("GetCurrentDateLaggedWeekSummary")
            .WithTags("Weather Summary");

        group.MapGet("lagged-date", HandlerAsync)
            .Produces<IResult>();
        
        return group;
    }

    
    private static async Task<Results<Ok<AggregatedWeeklyHistoricalWeatherSnapshot>,InternalServerError<Exception>>> HandlerAsync([AsParameters] GetWeatherSummaryRequest _request, [FromServices] DateExtraction _dateExtraction, [FromServices] IAggregatedWeeklyHistoricalWeatherRepository _repository, CancellationToken ctk)
    {
        try
        {
            var dateParts = _dateExtraction.ExtractCurrentDateAndLaggedDate(_request.DateSelected);
            var res = await _repository.GetWeeklyHistoricalWeatherSnapshotAsync(_request.PsgcCode, dateParts.ISOYear, dateParts.ISOWeek);
            return TypedResults.Ok(res);
        }
        catch (Exception e)
        {
            return TypedResults.InternalServerError(e);
        }
         
    }
}