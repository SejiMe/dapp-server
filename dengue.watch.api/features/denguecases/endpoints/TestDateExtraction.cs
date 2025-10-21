using dengue.watch.api.features.denguecases.services;

namespace dengue.watch.api.features.denguecases.endpoints;

public class TestDateExtraction : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dengue-cases")
            .WithSummary("Test Date Extraction Service")
            .WithName("TestDateExtraction")
            .WithTags("Dengue Cases");

        group.MapGet("test-date-extraction", Handler);
        
        return group;
    }

    public record DateExtractionResponse(int year, int week, int laggedISOYear, int laggedISOWeek);

    public static Task<DateExtractionResponse> Handler([FromServices] DateExtraction _extractDate,[FromQuery]DateOnly date)
    {

        var res = _extractDate.ExtractCurrentDateAndLaggedDate(date);
        return Task.FromResult(new DateExtractionResponse(res.ISOYear, res.ISOWeek, res.LaggedYear, res.LaggedWeek));
    }

}