using dengue.watch.api.features.weatherpooling.services;
using dengue.watch.api.features.weatherpooling.jobs;
using Quartz;
using Microsoft.Extensions.Options;
using dengue.watch.api.features.weatherpooling.options;
using System.Runtime.InteropServices;
using Serilog;

namespace dengue.watch.api.features.weatherpooling;

public class WeatherPoolingFeature : IFeature
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
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
            timeZoneId = "Asia/Singapore";
            Log.Information("Running on Linux/macOS");
        }
        services.AddOptions<DailyWeatherPoolingJobOptions>()
            .Bind(configuration.GetSection(DailyWeatherPoolingJobOptions.SectionName));

        // Register job with DI
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("Fetch 1 day Historical Weather Data");
            q.AddJob<DailyWeatherPoolingJob>(opts => opts.WithIdentity(jobKey));

            var jobOptions = configuration
                .GetSection(DailyWeatherPoolingJobOptions.SectionName)
                .Get<DailyWeatherPoolingJobOptions>();
            var cron = string.IsNullOrWhiteSpace(jobOptions?.Cron) ? "0 30 16 * * ?" : jobOptions!.Cron;

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("DailyWeatherPoolingJob-trigger")
                .WithCronSchedule(cron,
                    // x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId))
        x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"))
                ));
        });
        return services;
    }
}