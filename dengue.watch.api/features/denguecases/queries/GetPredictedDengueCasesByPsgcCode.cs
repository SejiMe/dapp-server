namespace dengue.watch.api.features.denguecases.queries
{
    public class GetDengueCasesByPsgcCode : IEndpoint
    {
        public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
        {
            
            var group = app.MapGroup("dengue-cases")
            .WithSummary("Get Prediction by Psgc and Paginated")
            .WithTags("Dengue Cases");

            
            group.MapGet("predictions/{psgccode}", Handler)
                .Produces<IResult>();
            return group;
            
        }

        // Default to 6 or 6 iso weeks
        public record FilterParameters(int page_number = 1, int page_size = 6);
            
        private static async ValueTask<IResult> Handler(
        string psgccode,
            [FromServices] DateExtraction _dateExtraction,
            [AsParameters] FilterParameters _params,
            [FromServices] ApplicationDbContext _db,
            [FromServices] IAggregatedWeeklyHistoricalWeatherRepository _repository,
            CancellationToken cancellation = default
        )
        {
            // TODO fetch all 4
        try
        {
            

            var bgyName = _db.AdministrativeAreas
                .Where(p => p.PsgcCode == psgccode)
                .Select(p => p.Name)
                .SingleOrDefault();

            if (bgyName is null)
                throw new NotFoundException("Barangay Doesn't Exist");

            // check if it exists 
            List<GetDenguePrediction> data = _db.PredictedWeeklyDengues.Where(p =>
                    p.PsgcCode == psgccode)
                    .OrderByDescending(ppp => ppp.PredictedIsoWeek)
                    .OrderByDescending(pp => pp.PredictedIsoYear)
                    .Skip((_params.page_number - 1) * _params.page_size)
                    .Take(_params.page_size)
                    .Select(p =>
                    
                        new GetDenguePrediction
                        (
                            psgccode, 
                            bgyName,
                            p.MonthName ?? string.Empty,
                            p.PredictedIsoYear, 
                            p.PredictedIsoWeek, 
                            p.LaggedIsoWeek, 
                            p.LaggedIsoYear, 
                            p.PredictedValue,
                            p.ProbabilityOfOutbreak
                        )
                    )
                    .ToList();

            if (data == null)
                throw new NotFoundException("Prediction Doesn't Exist");

            GetDenguePredictionResponse response = new(data.Count,_params.page_number, data);
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

        public record GetDenguePredictionRequest();

        public record GetDenguePrediction(string psgccode, string barangay_name, string month_name, int iso_year, int iso_week, int lagged_week, int lagged_year ,float value_predicted, double outbreak_probability);
        
        public record GetDenguePredictionResponse(int page_number, int page_size, List<GetDenguePrediction> predictions);
        
    }
}