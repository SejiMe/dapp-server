namespace dengue.watch.api.common.interfaces;

/// <summary>
/// Marker interface for feature discovery through reflection
/// </summary>
public interface IFeature
{
    /// <summary>
    /// Configure the feature services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The configured service collection</returns>
    static abstract IServiceCollection ConfigureServices(IServiceCollection services);
}
