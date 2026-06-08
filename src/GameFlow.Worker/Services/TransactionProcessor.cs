using System.Text.Json;
using GameFlow.Shared.Entities;
using GameFlow.Shared.Enums;
using GameFlow.Shared.Messaging;
using GameFlow.Shared.Persistence;
using GameFlow.Shared.Search;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Worker.Services;

public sealed class TransactionProcessor(
    GameFlowDbContext dbContext,
    ISignalRDispatcher signalRDispatcher,
    ISearchIndexWriter searchIndexWriter,
    ILogger<TransactionProcessor> logger)
{
    public async Task ProcessAsync(TransactionCommandMessage message, CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .AsNoTracking()
            .Include(x => x.Player)
            .Include(x => x.Game)
            .FirstOrDefaultAsync(x => x.Id == message.TransactionId, cancellationToken);

        if (transaction is null)
        {
            throw new InvalidOperationException($"Transaction {message.TransactionId} was not found.");
        }

        try
        {
            await dbContext.Transactions
                .Where(x => x.Id == transaction.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, TransactionStatus.Processing),
                    cancellationToken);

            transaction.Status = TransactionStatus.Processing;
            dbContext.TransactionEvents.Add(CreateEvent(transaction, "TransactionProcessing", "Worker picked up transaction from RabbitMQ."));
            dbContext.AuditLogs.Add(CreateAudit("TransactionProcessing", transaction));
            await dbContext.SaveChangesAsync(cancellationToken);

            await signalRDispatcher.BroadcastAsync(CreateLifecycleEvent(transaction, "processing", "Transaction is being processed."), cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

            if (transaction.Amount >= 5000m)
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.FailureReason = "Risk threshold exceeded for automated settlement.";
                transaction.ProcessedAtUtc = DateTime.UtcNow;
                await dbContext.Transactions
                    .Where(x => x.Id == transaction.Id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, TransactionStatus.Failed)
                        .SetProperty(x => x.FailureReason, transaction.FailureReason)
                        .SetProperty(x => x.ProcessedAtUtc, transaction.ProcessedAtUtc),
                        cancellationToken);

                dbContext.TransactionEvents.Add(CreateEvent(transaction, "TransactionFailed", transaction.FailureReason));
                dbContext.AuditLogs.Add(CreateAudit("TransactionFailed", transaction));
            }
            else
            {
                transaction.Status = TransactionStatus.Completed;
                transaction.ProcessedAtUtc = DateTime.UtcNow;
                await dbContext.Transactions
                    .Where(x => x.Id == transaction.Id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, TransactionStatus.Completed)
                        .SetProperty(x => x.ProcessedAtUtc, transaction.ProcessedAtUtc),
                        cancellationToken);

                dbContext.TransactionEvents.Add(CreateEvent(transaction, "TransactionCompleted", "Worker settled transaction and published downstream events."));
                dbContext.AuditLogs.Add(CreateAudit("TransactionCompleted", transaction));
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            await signalRDispatcher.BroadcastAsync(
                CreateLifecycleEvent(
                    transaction,
                    transaction.Status == TransactionStatus.Completed ? "completed" : "failed",
                    transaction.Status == TransactionStatus.Completed
                        ? "Transaction completed successfully."
                        : transaction.FailureReason ?? "Transaction failed."),
                cancellationToken);

            await searchIndexWriter.IndexAsync(
                new IndexedTransactionDocument
                {
                    TransactionId = transaction.Id,
                    ExternalTransactionId = transaction.ExternalTransactionId,
                    CorrelationId = transaction.CorrelationId,
                    PlayerExternalId = transaction.Player!.ExternalPlayerId,
                    PlayerUsername = transaction.Player.Username,
                    GameExternalId = transaction.Game!.ExternalGameId,
                    GameName = transaction.Game.Name,
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    Status = transaction.Status,
                    Type = transaction.Type.ToString(),
                    FailureReason = transaction.FailureReason,
                    CreatedAtUtc = transaction.CreatedAtUtc,
                    ProcessedAtUtc = transaction.ProcessedAtUtc
                },
                cancellationToken);

            logger.LogInformation("Processed transaction {ExternalTransactionId} with final status {Status}.", transaction.ExternalTransactionId, transaction.Status);
        }
        catch (Exception exception)
        {
            dbContext.ChangeTracker.Clear();

            dbContext.FailedMessages.Add(new FailedMessage
            {
                MessageType = nameof(TransactionCommandMessage),
                Payload = JsonSerializer.Serialize(message),
                Reason = exception.Message,
                RetryCount = 1,
                FirstFailedAtUtc = DateTime.UtcNow,
                LastFailedAtUtc = DateTime.UtcNow
            });

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception failedMessageException)
            {
                logger.LogError(failedMessageException, "Failed to persist failed message record for transaction {ExternalTransactionId}.", message.ExternalTransactionId);
            }

            throw;
        }
    }

    private static TransactionEvent CreateEvent(Transaction transaction, string eventType, string message)
    {
        return new TransactionEvent
        {
            TransactionId = transaction.Id,
            EventType = eventType,
            Message = message,
            PayloadJson = JsonSerializer.Serialize(new
            {
                transaction.ExternalTransactionId,
                transaction.CorrelationId,
                transaction.Amount,
                transaction.Status
            }),
            OccurredAtUtc = DateTime.UtcNow
        };
    }

    private static AuditLog CreateAudit(string action, Transaction transaction)
    {
        return new AuditLog
        {
            Action = action,
            Actor = "gameflow-worker",
            EntityType = nameof(Transaction),
            EntityId = transaction.ExternalTransactionId,
            DetailsJson = JsonSerializer.Serialize(new
            {
                transaction.CorrelationId,
                Status = transaction.Status.ToString(),
                transaction.Amount,
                transaction.FailureReason
            }),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static TransactionLifecycleEvent CreateLifecycleEvent(Transaction transaction, string stage, string message)
    {
        return new TransactionLifecycleEvent
        {
            TransactionId = transaction.Id,
            ExternalTransactionId = transaction.ExternalTransactionId,
            CorrelationId = transaction.CorrelationId,
            Status = transaction.Status,
            Stage = stage,
            Message = message,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            PlayerUsername = transaction.Player?.Username ?? "unknown-player",
            GameName = transaction.Game?.Name ?? "unknown-game",
            OccurredAtUtc = DateTime.UtcNow
        };
    }
}
