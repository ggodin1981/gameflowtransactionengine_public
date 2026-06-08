using System.Collections.Concurrent;

namespace GameFlow.SignalR.Services;

public sealed class ConnectionRegistry
{
    private readonly ConcurrentDictionary<string, byte> _connections = new();

    public int ActiveConnections => _connections.Count;

    public void Add(string connectionId)
    {
        _connections.TryAdd(connectionId, 0);
    }

    public void Remove(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }
}
