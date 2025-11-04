using dengue.watch.api.features.denguecases.services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.ML;

namespace dengue.watch.api.features.denguecases.endpoints;

public class GetPredictedDengueCasesByPsgcAndDate : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dengue-cases")
            .WithSummary("Get Prediction by Psgc and Date (yyyy-MM-dd)")
            .WithTags("Dengue Cases");

        group.MapGet("{psgccode}", Handler)
            .Produces<IResult>();
        return group;
    }
    
    public record GetDenguePredictionRequest(DateOnly dt);
    public record GetDenguePredictionResponse(string psgccode, string barangayName, int iso_year, int iso_week, int lagged_week, int lagged_year ,float valuePredicted);
    private static async Task<Results<Ok<GetDenguePredictionResponse>,NotFound<string>, BadRequest<string>, ProblemHttpResult>> Handler(
        string psgccode,
        [AsParameters] GetDenguePredictionRequest _request,
        [FromServices] DateExtraction _dateExtraction,
        [FromServices] ApplicationDbContext _db,
        [FromServices] IAggregatedWeeklyHistoricalWeatherRepository _repository,
        CancellationToken cancellation = default)
    {
        try
        {
            var dateParts = _dateExtraction.ExtractCurrentDateAndLaggedDate(_request.dt);

            var bgyName = _db.AdministrativeAreas
                .Where(p => p.PsgcCode == psgccode)
                .Select(p => p.Name)
                .SingleOrDefault();

            if (bgyName is null)
                throw new ValidationException("Barangay Doesn't Exist");

            // check if it exists 
            var data = _db.PredictedWeeklyDengues.Where(p =>
                    p.PsgcCode == psgccode && p.PredictedIsoWeek == dateParts.ISOWeek &&
                    p.PredictedIsoYear == dateParts.ISOYear)
                .SingleOrDefault();

            if (data == null)
                throw new NotFoundException("Prediction Doesn't Exist");

            GetDenguePredictionResponse response = new(psgccode, bgyName, dateParts.ISOYear, dateParts.ISOWeek, dateParts.LaggedWeek, dateParts.LaggedYear, data.PredictedValue);
            return TypedResults.Ok(response);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception e)
        {
            return TypedResults.Problem($"Cant Get Dengue Prediction Due to {e.Message}. {e.InnerException} {e.StackTrace}");
        }
    }
}