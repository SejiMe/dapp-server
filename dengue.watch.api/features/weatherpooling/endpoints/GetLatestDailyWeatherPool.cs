using Microsoft.AspNetCore.Http.HttpResults;

namespace dengue.watch.api.features.weatherpooling.endpoints
{
    public class GetLatestDailyWeatherPool : IEndpoint
    {
        public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("weatherpooling")
            .WithSummary("Get the latest weather pooled by date")
            .WithOpenApi();

            group.MapGet("latest", Handler)
            .WithName("Get Latest Weather Pooled")
            .WithTags("Weather Pooling")
            .Produces<DailyWeatherResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

            return group;
        }


        public record DailyWeatherResponse(DateTime date);
        private static async Task<Results<Ok<DailyWeatherResponse>, ProblemHttpResult>> Handler([FromServices] ApplicationDbContext _db)
        {
            try
            {
                var latestDate = _db.DailyWeather.OrderByDescending(p => p.Date).First();

                


                DailyWeatherResponse dailyWeather = new(date: latestDate.Date);

            
            return TypedResults.Ok(dailyWeather);
            }
            catch (System.Exception e)
            {
                var prob = new ProblemDetails()
                {
                    Detail = e.Message,
                    Status = StatusCodes.Status500InternalServerError

                };


                
                return TypedResults.Problem(prob);
            }
            
        }
    }
}