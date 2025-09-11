using dengue.watch.api.features.weatherpooling.services;
using dengue.watch.api.features.weatherpooling.jobs;
using Quartz;
using System.Runtime.InteropServices;
using Serilog;

namespace dengue.watch.api.features.weatherpooling;

public class WeatherPoolingFeature : IFeature
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        // TODO: Get Day minus 1 data from the API
        // services.AddScoped<WeatherDataProcessor>();
        services.AddScoped<IWeatherDataAPI, WeatherDataAPI>();
        services.AddScoped<IWeatherDateService, WeatherDateService>();
        services.AddScoped<WeatherDataProcessor>();

        string timeZoneId;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            timeZoneId = "Singapore Standard Time";
            Log.Information("Running on Windows");
        }
        else
        {
            timeZoneId = "Asia/Manila";
            Log.Information("Running on Linux/macOS");
        }
        // Register job with DI
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("DailyWeatherPoolingJob");
            q.AddJob<DailyWeatherPoolingJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("DailyWeatherPoolingJob-trigger")
                .WithCronSchedule("0 0 0 * * ?",
                    x =>x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId))
             // Every day at 12:00 AM
            ));
        });
        return services;
    }
}