using dengue.watch.api.common.interfaces;

namespace dengue.watch.api.features.advisories;

/// <summary>
/// Community Preventive Advisories feature registration
/// </summary>
public class AdvisoriesFeature : IFeature
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        return services;
    }
}
