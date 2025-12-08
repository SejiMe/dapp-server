using dengue.watch.api.features.denguecases.dtos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.denguecases.queries
{
    public class GetHistoricalDengueCasePerYear : IEndpoint
    {
        public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("dengue-cases")
            .WithTags("Dengue Cases")
            .WithSummary("Get History (recorded) Dengue Cases per Barangay as of 2014");

            group.MapGet("historical-year/{psgccode}", Handler);
            return group;
        }

        private static async Task<Results<Ok<HistoricalYearlyDengueCases>, NotFound<ProblemDetails>, ProblemHttpResult>> Handler(string psgccode,[FromServices] ILogger<GetHistoricalDengueCasePerYear> _logger, [FromServices] ApplicationDbContext _db)
        {
            try
            {

                var psgc = _db.AdministrativeAreas.SingleOrDefault(p => p.PsgcCode == psgccode);

                if (psgc == null)
                    throw new NotFoundException($"No results found for {psgccode}");

                int startYear = 2014;
                int currentYear = DateTime.Now.Year;

                int[] years = Enumerable.Range(startYear, currentYear - startYear + 1).ToArray();
                HistoricalYearlyDengueCases dengueCasesResults = new();
                dengueCasesResults.psgccode = psgccode;
                dengueCasesResults.recorded_cases = new List<YearlyTotalDengueCase>();


                foreach (var y in years)
                {
                    var count = _db.WeeklyDengueCases.Where(p => p.PsgcCode == psgccode && p.Year == y).Sum(p => p.CaseCount);
                    dengueCasesResults.recorded_cases.Add(new(y.ToString(), count));
                }

                return TypedResults.Ok(dengueCasesResults);

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

        }
    }
}