using Microsoft.AspNetCore.SignalR;

namespace dengue.watch.api.common.interfaces;

/// <summary>
/// Marker interface for SignalR hub discovery through reflection
/// </summary>
public interface IHub
{
    /// <summary>
    /// Configure the SignalR hub routes
    /// </summary>
    /// <param name="app">The web application builder</param>
    /// <returns>The configured web application</returns>
    static abstract IEndpointRouteBuilder MapHub(IEndpointRouteBuilder app);
}
