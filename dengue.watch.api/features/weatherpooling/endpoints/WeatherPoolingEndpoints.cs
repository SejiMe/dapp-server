using dengue.watch.api.common.interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Impl.Matchers;

namespace dengue.watch.api.features.weatherpooling.endpoints;

public partial class WeatherPoolingEndpoints : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weatherpooling")
            .WithTags("Weather Pooling")
            .WithOpenApi();

        group.MapGet("/health", GetQuartzHealth)
            .WithName("WeatherPoolingHealth")
            .WithSummary("Get Quartz scheduler health and job status");

        return app;
    }

   

    private static async Task<IResult> GetQuartzHealth([FromServices] ISchedulerFactory schedulerFactory)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var standby = scheduler.InStandbyMode;
        var started = scheduler.IsStarted;
        var shutdown = scheduler.IsShutdown;

        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<Quartz.JobKey>.AnyGroup());
        var triggers = await scheduler.GetTriggersOfJob(jobKeys.FirstOrDefault());

        return Results.Ok(new
        {
            started,
            standby,
            shutdown,
            jobs = jobKeys.Select(j => j.Name).ToArray(),
            nextFireTimes = triggers.Select(t => t.GetNextFireTimeUtc()?.UtcDateTime).ToArray()
        });
    }
}


