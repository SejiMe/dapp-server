using Quartz;
using Quartz.AspNetCore;

namespace dengue.watch.api.common.extensions;

public static class QuartzExtensions
{
    public static IServiceCollection AddQuartzExtension(this IServiceCollection services)
    {
        services.AddQuartz(op =>
        {
            op.UseSimpleTypeLoader();
            op.UseInMemoryStore();
            op.UseDefaultThreadPool(tp =>
            {
                tp.MaxConcurrency = 10;
            });
        });
        services.AddQuartzServer(q =>
        {
            q.WaitForJobsToComplete = true;
        });
        return services;
    }
}