namespace dengue.watch.api.common.interfaces;

/// <summary>
/// Marker interface for endpoint discovery through reflection
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Configure the endpoint routes and handlers
    /// </summary>
    /// <param name="app">The web application builder</param>
    /// <returns>The configured web application</returns>
    static abstract IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app);
}
