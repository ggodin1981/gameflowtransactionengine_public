using GameFlow.Shared.Messaging;
using GameFlow.Shared.Search;

namespace GameFlow.Worker.Services;

public interface ISignalRDispatcher
{
    Task BroadcastAsync(TransactionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken);
}

public interface ISearchIndexWriter
{
    Task IndexAsync(IndexedTransactionDocument document, CancellationToken cancellationToken);
}
