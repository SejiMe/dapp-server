namespace dengue.watch.api.features.denguecases;

public class DengueCasesFeature : IFeature
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        // services.AddScoped<IDengueCasesService, DengueCasesService>();
        return services;
    }
}