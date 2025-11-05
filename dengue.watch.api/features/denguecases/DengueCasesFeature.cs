using System.Runtime.InteropServices;
using dengue.watch.api.features.denguecases.jobs;
using dengue.watch.api.features.denguecases.services;
using Quartz;
using Serilog;

namespace dengue.watch.api.features.denguecases;

public class DengueCasesFeature : IFeature
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        return services;
    }
    
    public static IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // services.AddScoped<IDengueCasesService, DengueCasesService>();
        
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
        
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("PredictDengueCaseAllBarangay");
            q.AddJob<WednesdayPredictionJob>(opts => opts.WithIdentity(jobKey));
            
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("WeeklyPrediction-trigger")
                // .WithCronSchedule("0 30 17 ? * WED *",x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId))
                // )
                .WithCronSchedule("0 30 17 * * ?",
                x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila")))
                )
                ;
        });
        
        
        
        return services;
    }
}