using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dengue.watch.api.features.denguecases.dtos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.denguecases.endpoints
{
    public class GetHistoricalDengueCasePerYear : IEndpoint
    {
        public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/dengue-cases")
            .WithTags("Dengue Cases")
            .WithSummary("Get History (recorded) Dengue Cases per Barangay as of 2014");

            group.MapGet("historical/{psgccode}", Handler);
            return group;
        }

        private static async Task<Results<Ok<HistoricalDengueCases>, NotFound<ProblemDetails>, ProblemHttpResult>> Handler(string psgccode,[FromServices] ILogger<GetHistoricalDengueCasePerYear> _logger, [FromServices] ApplicationDbContext _db)
        {
            try
            {


                var psgc = _db.AdministrativeAreas.SingleOrDefault(p => p.PsgcCode == psgccode);

                if (psgc == null)
                    throw new NotFoundException($"No results found for {psgccode}");

                int startYear = 2014;
                int currentYear = DateTime.Now.Year;

                int[] years = Enumerable.Range(startYear, currentYear - startYear + 1).ToArray();
                HistoricalDengueCases dengueCasesResults = new();
                dengueCasesResults.psgccode = psgccode;
                dengueCasesResults.TotalDengueCases = new List<YearlyTotalDengueCase>();


                foreach (var y in years)
                {
                    var count = _db.WeeklyDengueCases.Where(p => p.PsgcCode == psgccode && p.Year == y).Sum(p => p.CaseCount);
                    dengueCasesResults.TotalDengueCases.Add(new(y.ToString(), count));
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
            
            return default;
        }
    }
}