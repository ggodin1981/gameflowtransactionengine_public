using GameFlow.SignalR.Services;
using Microsoft.AspNetCore.SignalR;

namespace GameFlow.SignalR.Hubs;

public sealed class TransactionHub(ConnectionRegistry connectionRegistry) : Hub
{
    public override Task OnConnectedAsync()
    {
        connectionRegistry.Add(Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        connectionRegistry.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
