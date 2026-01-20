using dengue.watch.api.features.denguecases.dtos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.denguecases.queries
{
    public class GetAllHistoricalDengueCasePerYear : IEndpoint
    {
        public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("dengue-cases")
            .WithTags("Dengue Cases")
            .WithSummary("Get History (recorded) Dengue Cases all Barangay as of 2014");

            group.MapGet("historical-year", Handler);
            return group;
        }

        private static async Task<Results<Ok<HistoricalYearlyDengueCases>, ProblemHttpResult>> Handler([FromServices] ILogger<GetAllHistoricalDengueCasePerYear> _logger, [FromServices] ApplicationDbContext _db)
        {
            try
            {
                int startYear = 2014;
                int currentYear = DateTime.Now.Year;

                int[] years = Enumerable.Range(startYear, currentYear - startYear + 1).ToArray();
                HistoricalYearlyDengueCases dengueCasesResults = new();
                dengueCasesResults.psgccode = "";
                dengueCasesResults.recorded_cases = new List<YearlyTotalDengueCase>();


                
                var cases = _db.WeeklyDengueCases
                .GroupBy(wkd => wkd.Year)
                .Select(p => new YearlyTotalDengueCase
                        (
                        p.Key.ToString(), p.Sum(x => x.CaseCount)
                        )
                    )
                .ToList()
                .OrderBy(pz => pz.year);
                
                dengueCasesResults.recorded_cases.AddRange(cases);
                dengueCasesResults.psgccode = null;

                return TypedResults.Ok(dengueCasesResults);

            }
            catch (Exception e)
            {

                _logger.LogError(e.Message, e);
                return TypedResults.Problem(e.Message, e.StackTrace, 500, e.InnerException?.ToString());
            }
            
        }
    }
}


