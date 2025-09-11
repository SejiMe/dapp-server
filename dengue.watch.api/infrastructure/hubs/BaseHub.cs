using Microsoft.AspNetCore.SignalR;
using dengue.watch.api.common.interfaces;

namespace dengue.watch.api.infrastructure.hubs;

/// <summary>
/// Base SignalR hub with common functionality
/// </summary>
public abstract class BaseHub : Hub
{
    protected ILogger Logger { get; }

    protected BaseHub(ILogger logger)
    {
        Logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        Logger.LogInformation("Client {ConnectionId} connected to {HubName}", 
            Context.ConnectionId, GetType().Name);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Logger.LogInformation("Client {ConnectionId} disconnected from {HubName}. Exception: {Exception}", 
            Context.ConnectionId, GetType().Name, exception?.Message);
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific group
    /// </summary>
    /// <param name="groupName">Name of the group to join</param>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Logger.LogInformation("Client {ConnectionId} joined group {GroupName}", 
            Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave a specific group
    /// </summary>
    /// <param name="groupName">Name of the group to leave</param>
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        Logger.LogInformation("Client {ConnectionId} left group {GroupName}", 
            Context.ConnectionId, groupName);
    }


}
