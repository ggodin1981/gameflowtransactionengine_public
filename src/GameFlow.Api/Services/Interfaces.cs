using GameFlow.Api.Models;
using GameFlow.Shared.Contracts.Audit;
using GameFlow.Shared.Contracts.Dashboard;
using GameFlow.Shared.Contracts.Players;
using GameFlow.Shared.Contracts.Transactions;

namespace GameFlow.Api.Services;

public interface ITransactionCommandService
{
    Task<TransactionAcceptedResponse> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken);
}

public interface ITransactionQueryService
{
    Task<TransactionSearchItem?> GetByExternalTransactionIdAsync(string externalTransactionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionSearchItem>> SearchAsync(TransactionSearchCriteria criteria, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditLogDto>> GetAuditLogsAsync(CancellationToken cancellationToken);
}

public interface IDashboardQueryService
{
    Task<DashboardOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken);
}

public interface IPlayerProfileService
{
    Task<PlayerProfileResponse?> GetByExternalIdAsync(string externalPlayerId, CancellationToken cancellationToken);
}

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken);
}

public interface ITransactionProcessingQueue
{
    ValueTask EnqueueAsync(GameFlow.Shared.Messaging.TransactionCommandMessage message, CancellationToken cancellationToken);
    ValueTask<GameFlow.Shared.Messaging.TransactionCommandMessage> DequeueAsync(CancellationToken cancellationToken);
}

public interface ITransactionLifecycleNotifier
{
    Task BroadcastAsync(GameFlow.Shared.Messaging.TransactionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken);
}
