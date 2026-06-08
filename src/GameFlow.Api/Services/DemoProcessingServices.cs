using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using GameFlow.Api.Options;
using GameFlow.Shared.Entities;
using GameFlow.Shared.Enums;
using GameFlow.Shared.Messaging;
using GameFlow.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameFlow.Api.Services;

public sealed class InMemoryTransactionQueue : ITransactionProcessingQueue
{
    private readonly Channel<TransactionCommandMessage> _channel = Channel.CreateUnbounded<TransactionCommandMessage>();

    public ValueTask EnqueueAsync(TransactionCommandMessage message, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(message, cancellationToken);

    public ValueTask<TransactionCommandMessage> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);
}

public sealed class InMemoryTransactionPublisher(
    ITransactionProcessingQueue queue,
    ILogger<InMemoryTransactionPublisher> logger) : IRabbitMqPublisher
{
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken)
    {
        if (message is not TransactionCommandMessage command)
        {
            throw new InvalidOperationException($"Unsupported in-memory message type {typeof(T).Name}.");
        }

        await queue.EnqueueAsync(command, cancellationToken);
        logger.LogInformation("Queued transaction {ExternalTransactionId} for in-memory demo processing.", command.ExternalTransactionId);
    }
}

public sealed class SignalRLifecycleNotifier(
    HttpClient httpClient,
    IOptions<SignalRServiceOptions> options,
    ILogger<SignalRLifecycleNotifier> logger) : ITransactionLifecycleNotifier
{
    private readonly SignalRServiceOptions _options = options.Value;

    public async Task BroadcastAsync(TransactionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return;
        }

        var response = await httpClient.PostAsJsonAsync("internal/events/transactions", lifecycleEvent, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("SignalR event dispatch failed for {ExternalTransactionId} with status {StatusCode}.", lifecycleEvent.ExternalTransactionId, response.StatusCode);
        }
    }
}

public sealed class DemoTransactionProcessingWorker(
    IServiceProvider serviceProvider,
    ITransactionProcessingQueue queue,
    ILogger<DemoTransactionProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Demo-mode transaction worker is running in-process.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await queue.DequeueAsync(stoppingToken);
                using var scope = serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<DemoTransactionProcessor>();
                await processor.ProcessAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Demo-mode transaction worker failed to process a message.");
            }
        }
    }
}

public sealed class DemoTransactionProcessor(
    GameFlowDbContext dbContext,
    ITransactionLifecycleNotifier lifecycleNotifier,
    ILogger<DemoTransactionProcessor> logger)
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
            dbContext.TransactionEvents.Add(CreateEvent(transaction, "TransactionProcessing", "Demo worker picked up transaction for processing."));
            dbContext.AuditLogs.Add(CreateAudit("TransactionProcessing", transaction));
            await dbContext.SaveChangesAsync(cancellationToken);

            await lifecycleNotifier.BroadcastAsync(CreateLifecycleEvent(transaction, "processing", "Transaction is being processed."), cancellationToken);
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

                dbContext.TransactionEvents.Add(CreateEvent(transaction, "TransactionCompleted", "Demo worker settled transaction successfully."));
                dbContext.AuditLogs.Add(CreateAudit("TransactionCompleted", transaction));
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            await lifecycleNotifier.BroadcastAsync(
                CreateLifecycleEvent(
                    transaction,
                    transaction.Status == TransactionStatus.Completed ? "completed" : "failed",
                    transaction.Status == TransactionStatus.Completed
                        ? "Transaction completed successfully."
                        : transaction.FailureReason ?? "Transaction failed."),
                cancellationToken);

            logger.LogInformation("Demo-mode processed transaction {ExternalTransactionId} with final status {Status}.", transaction.ExternalTransactionId, transaction.Status);
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
                logger.LogError(failedMessageException, "Failed to persist demo-mode failed message for transaction {ExternalTransactionId}.", message.ExternalTransactionId);
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
            Actor = "gameflow-demo-worker",
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
