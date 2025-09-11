using dengue.watch.api.common.interfaces;
using dengue.watch.api.infrastructure.database;
using Microsoft.EntityFrameworkCore;

namespace dengue.watch.api.features.denguealerts;

/// <summary>
/// Feature configuration for dengue alerts
/// </summary>
public class DengueAlertFeature : IFeature
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        // Register the service
        services.AddScoped<IDengueAlertService, DengueAlertService>();

        return services;
    }
}
