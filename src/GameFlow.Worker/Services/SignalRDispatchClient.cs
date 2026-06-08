using System.Net.Http.Json;
using GameFlow.Shared.Messaging;

namespace GameFlow.Worker.Services;

public sealed class SignalRDispatchClient(HttpClient httpClient, ILogger<SignalRDispatchClient> logger) : ISignalRDispatcher
{
    public async Task BroadcastAsync(TransactionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("internal/events/transactions", lifecycleEvent, cancellationToken);
        response.EnsureSuccessStatusCode();
        logger.LogDebug("Dispatched SignalR lifecycle event {ExternalTransactionId} ({Stage}).", lifecycleEvent.ExternalTransactionId, lifecycleEvent.Stage);
    }
}
