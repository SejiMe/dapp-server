using Quartz;
using Microsoft.AspNetCore.Mvc;
using dengue.watch.api.common.interfaces;

namespace dengue.watch.api.features.weatherpooling.endpoints;

public class ManualTriggerAPIPooling : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weatherpooling")
            .WithTags("Weather Pooling")
            .WithOpenApi();

        group.MapPost("/manual-trigger", ManuallyTriggerWeatherPooling)
        .WithName("ManualTriggerWeatherPooling")
        .WithSummary("Manually trigger the weather pooling job")
        .WithDescription("Manually trigger the weather pooling job")
        .Produces(200)
        .Produces(400);

        return group;
    }

    private static async Task<IResult> ManuallyTriggerWeatherPooling([FromServices] ISchedulerFactory schedulerFactory)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        JobKey jobKey = new("DailyWeatherPoolingJob");
        await scheduler.TriggerJob(jobKey);
        return Results.Ok("Weather pooling job triggered");
    }
}