using dengue.watch.api.common.interfaces;

namespace dengue.watch.api.features.administrativeareas;

public class AdministrativeAreaFeature : IFeature
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IAdministrativeAreaService, AdministrativeAreaService>();
        return services;
    }
}


