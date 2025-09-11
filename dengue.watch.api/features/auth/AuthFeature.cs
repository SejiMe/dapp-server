using dengue.watch.api.common.interfaces;

namespace dengue.watch.api.features.auth;

/// <summary>
/// Authentication feature registration
/// </summary>
public class AuthFeature : IFeature
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        // Services are registered in Program.cs through SupabaseExtensions
        // This feature just groups the auth endpoints together
        return services;
    }
}
