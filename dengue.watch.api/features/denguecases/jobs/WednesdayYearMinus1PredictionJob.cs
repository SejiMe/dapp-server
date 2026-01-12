using Quartz;

namespace dengue.watch.api.features.denguecases.jobs;

public class WednesdayYearMinus1PredictionJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        /*
         * REVIEW: Lalagyan paba ng ganito?
         * Kapag example its 2026 supposedly may prediction na ng 2027
         * if na kumpleto na ang data ng 2025 dapat magkaroon ng remapping ng value based either sa lagged minus 1 year 
         * or lagged minus 2 weeks
         */ 
        
        
        
        return Task.CompletedTask;
    }
}