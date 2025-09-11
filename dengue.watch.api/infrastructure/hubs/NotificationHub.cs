using Microsoft.AspNetCore.SignalR;
using dengue.watch.api.common.interfaces;

namespace dengue.watch.api.infrastructure.hubs;

/// <summary>
/// SignalR hub for general notifications
/// </summary>
public class NotificationHub : BaseHub, IHub
{
    public NotificationHub(ILogger<NotificationHub> logger) : base(logger)
    {
    }

    /// <summary>
    /// Send a notification to all connected clients
    /// </summary>
    /// <param name="message">The notification message</param>
    public async Task SendNotificationToAll(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", new
        {
            Message = message,
            Timestamp = DateTime.UtcNow,
            Type = "General"
        });

        Logger.LogInformation("Notification sent to all clients: {Message}", message);
    }

    /// <summary>
    /// Send a notification to a specific group
    /// </summary>
    /// <param name="groupName">The group name</param>
    /// <param name="message">The notification message</param>
    public async Task SendNotificationToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("ReceiveNotification", new
        {
            Message = message,
            Timestamp = DateTime.UtcNow,
            Type = "Group",
            Group = groupName
        });

        Logger.LogInformation("Notification sent to group {GroupName}: {Message}", groupName, message);
    }

    /// <summary>
    /// Send a notification to a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="message">The notification message</param>
    public async Task SendNotificationToUser(string userId, string message)
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", new
        {
            Message = message,
            Timestamp = DateTime.UtcNow,
            Type = "Personal"
        });

        Logger.LogInformation("Notification sent to user {UserId}: {Message}", userId, message);
    }

    /// <summary>
    /// Map the hub to the endpoint route builder
    /// </summary>
    public static IEndpointRouteBuilder MapHub(IEndpointRouteBuilder app)
    {
        app.MapHub<NotificationHub>("/hubs/notifications");
        return app;
    }
}
