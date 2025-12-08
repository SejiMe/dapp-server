using dengue.watch.api.features.denguecases.dtos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.denguecases.queries
{
    public class GetHistoricalDengueCaseByPsgcCode : IEndpoint
    {
        public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("dengue-cases")
            .WithTags("Dengue Cases")
            .WithSummary("Get paginated Historical Data recorded Dengue Cases per Barangay as of 2014 & 2024");

            group.MapGet("historical/{psgccode}", Handler);
            return group;
        }

        public record FilterParameter(int page_size = 6, int page_number = 1);

        public record HistoricalWeeklyDengueCaseResponse(int page_number, int page_size, List<HistoricalWeeklyDengueCases> dengue_cases);
        private static async Task<Results<Ok<HistoricalWeeklyDengueCaseResponse>, NotFound<ProblemDetails>, ProblemHttpResult>> Handler(
            string psgccode,
            [AsParameters] FilterParameter _params,
            [FromServices] ILogger<GetHistoricalDengueCasePerYear> _logger, 
            [FromServices] ApplicationDbContext _db)
        {
            try
            {
                var psgc = _db.AdministrativeAreas.SingleOrDefault(p => p.PsgcCode == psgccode);

                if (psgc == null)
                    throw new NotFoundException($"No results found for {psgccode}");

                var results = _db.WeeklyDengueCases.Where(p => p.PsgcCode == psgccode)
                .Skip((_params.page_number - 1 ) * _params.page_size)
                .Take(_params.page_size)
                .Select(p => new HistoricalWeeklyDengueCases(p.Year, p.WeekNumber,p.CaseCount))
                .ToList();

                HistoricalWeeklyDengueCaseResponse response = new(_params.page_number, _params.page_size, results);

                return TypedResults.Ok(response);

            }
            catch (NotFoundException e)
            {
                throw;
            }
            catch (Exception e)
            {

                _logger.LogError(e.Message, e);
                return TypedResults.Problem(e.Message, e.StackTrace, 500, e.InnerException?.ToString());
            }
            
            return default;
        }
    }
}