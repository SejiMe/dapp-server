using dengue.watch.api.infrastructure.hubs;
using dengue.watch.api.common.interfaces;
using Microsoft.AspNetCore.SignalR;

namespace dengue.watch.api.features.denguealerts;

/// <summary>
/// SignalR hub for dengue alert notifications
/// </summary>
public class DengueAlertHub : BaseHub, IHub
{
    public DengueAlertHub(ILogger<DengueAlertHub> logger) : base(logger)
    {
    }

    /// <summary>
    /// Subscribe to alerts for a specific location
    /// </summary>
    /// <param name="location">The location to subscribe to</param>
    public async Task SubscribeToLocation(string location)
    {
        var groupName = $"alerts-{location.ToLowerInvariant()}";
        await JoinGroup(groupName);
        
        await Clients.Caller.SendAsync("SubscriptionConfirmed", new
        {
            Location = location,
            GroupName = groupName,
            Message = $"Subscribed to alerts for {location}"
        });
    }

    /// <summary>
    /// Unsubscribe from alerts for a specific location
    /// </summary>
    /// <param name="location">The location to unsubscribe from</param>
    public async Task UnsubscribeFromLocation(string location)
    {
        var groupName = $"alerts-{location.ToLowerInvariant()}";
        await LeaveGroup(groupName);
        
        await Clients.Caller.SendAsync("UnsubscriptionConfirmed", new
        {
            Location = location,
            GroupName = groupName,
            Message = $"Unsubscribed from alerts for {location}"
        });
    }

    /// <summary>
    /// Broadcast new alert to subscribers
    /// </summary>
    /// <param name="alert">The dengue alert</param>
    public async Task BroadcastNewAlert(DengueAlert alert)
    {
        var groupName = $"alerts-{alert.Location.ToLowerInvariant()}";
        
        await Clients.Group(groupName).SendAsync("NewAlert", new
        {
            Id = alert.Id,
            Location = alert.Location,
            Description = alert.Description,
            Level = alert.Level.ToString(),
            CreatedAt = alert.CreatedAt,
            Type = "NewAlert"
        });

        // Also send to general notifications
        await Clients.All.SendAsync("GeneralAlert", new
        {
            Message = $"New {alert.Level} alert in {alert.Location}: {alert.Description}",
            Location = alert.Location,
            Level = alert.Level.ToString(),
            Timestamp = alert.CreatedAt
        });

        Logger.LogInformation("New alert broadcasted for location {Location} with level {Level}", 
            alert.Location, alert.Level);
    }

    /// <summary>
    /// Broadcast alert resolution to subscribers
    /// </summary>
    /// <param name="alert">The resolved dengue alert</param>
    public async Task BroadcastAlertResolved(DengueAlert alert)
    {
        var groupName = $"alerts-{alert.Location.ToLowerInvariant()}";
        
        await Clients.Group(groupName).SendAsync("AlertResolved", new
        {
            Id = alert.Id,
            Location = alert.Location,
            ResolvedAt = alert.ResolvedAt,
            Type = "AlertResolved"
        });

        Logger.LogInformation("Alert resolution broadcasted for location {Location}", alert.Location);
    }

    /// <summary>
    /// Map the hub to the endpoint route builder
    /// </summary>
    public static IEndpointRouteBuilder MapHub(IEndpointRouteBuilder app)
    {
        app.MapHub<DengueAlertHub>("/hubs/dengue-alerts");
        return app;
    }
}
