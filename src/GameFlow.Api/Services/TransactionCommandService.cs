using System.Text.Json;
using GameFlow.Shared.Contracts.Transactions;
using GameFlow.Shared.Entities;
using GameFlow.Shared.Enums;
using GameFlow.Shared.Messaging;
using GameFlow.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameFlow.Api.Services;

public sealed class TransactionCommandService(
    GameFlowDbContext dbContext,
    IRabbitMqPublisher publisher,
    ILogger<TransactionCommandService> logger) : ITransactionCommandService
{
    public async Task<TransactionAcceptedResponse> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var normalizedRequest = Normalize(request);

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var existingTransaction = await GetExistingTransactionAsync(normalizedRequest.ExternalTransactionId, cancellationToken);
            if (existingTransaction is not null)
            {
                EnsureMatchingRequest(existingTransaction, normalizedRequest);
                logger.LogInformation("Duplicate submission received for transaction {ExternalTransactionId}. Returning existing transaction.", existingTransaction.ExternalTransactionId);
                return MapAcceptedResponse(existingTransaction);
            }

            var player = await dbContext.Players
                .FirstOrDefaultAsync(x => x.ExternalPlayerId == normalizedRequest.PlayerExternalId, cancellationToken)
                ?? new Player
                {
                    ExternalPlayerId = normalizedRequest.PlayerExternalId,
                    Username = normalizedRequest.PlayerUsername,
                    Country = normalizedRequest.Country,
                    Currency = normalizedRequest.Currency,
                    CreatedAtUtc = DateTime.UtcNow
                };

            player.Username = normalizedRequest.PlayerUsername;
            player.Country = normalizedRequest.Country;
            player.Currency = normalizedRequest.Currency;

            if (dbContext.Entry(player).State == EntityState.Detached)
            {
                dbContext.Players.Add(player);
            }

            var game = await dbContext.Games
                .FirstOrDefaultAsync(x => x.ExternalGameId == normalizedRequest.GameExternalId, cancellationToken)
                ?? new Game
                {
                    ExternalGameId = normalizedRequest.GameExternalId,
                    Name = normalizedRequest.GameName,
                    Provider = normalizedRequest.Provider,
                    CreatedAtUtc = DateTime.UtcNow
                };

            game.Name = normalizedRequest.GameName;
            game.Provider = normalizedRequest.Provider;

            if (dbContext.Entry(game).State == EntityState.Detached)
            {
                dbContext.Games.Add(game);
            }

            var createdAtUtc = DateTime.UtcNow;
            var transaction = new Transaction
            {
                ExternalTransactionId = normalizedRequest.ExternalTransactionId,
                CorrelationId = Guid.NewGuid().ToString("N"),
                Player = player,
                Game = game,
                Amount = normalizedRequest.Amount,
                Currency = normalizedRequest.Currency,
                Status = TransactionStatus.Pending,
                Type = normalizedRequest.Type,
                CreatedAtUtc = createdAtUtc
            };

            transaction.Events.Add(new TransactionEvent
            {
                EventType = "TransactionCreated",
                Message = "Transaction accepted by API and queued for processing.",
                PayloadJson = JsonSerializer.Serialize(normalizedRequest),
                OccurredAtUtc = createdAtUtc
            });

            dbContext.Transactions.Add(transaction);
            dbContext.AuditLogs.Add(new AuditLog
            {
                Action = "TransactionAccepted",
                Actor = "gameflow-api",
                EntityType = nameof(Transaction),
                EntityId = transaction.ExternalTransactionId,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    transaction.CorrelationId,
                    normalizedRequest.PlayerExternalId,
                    normalizedRequest.GameExternalId,
                    normalizedRequest.Amount,
                    normalizedRequest.Type
                }),
                CreatedAtUtc = createdAtUtc
            });

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception) && attempt < 2)
            {
                logger.LogWarning(exception, "Unique constraint conflict while creating transaction {ExternalTransactionId}. Retrying with fresh database state.", normalizedRequest.ExternalTransactionId);
                dbContext.ChangeTracker.Clear();
                continue;
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                logger.LogWarning(exception, "Unique constraint conflict persisted for transaction {ExternalTransactionId}. Loading existing row.", normalizedRequest.ExternalTransactionId);
                dbContext.ChangeTracker.Clear();

                existingTransaction = await GetExistingTransactionAsync(normalizedRequest.ExternalTransactionId, cancellationToken);
                if (existingTransaction is not null)
                {
                    EnsureMatchingRequest(existingTransaction, normalizedRequest);
                    return MapAcceptedResponse(existingTransaction);
                }

                throw;
            }

            var message = new TransactionCommandMessage
            {
                TransactionId = transaction.Id,
                ExternalTransactionId = transaction.ExternalTransactionId,
                CorrelationId = transaction.CorrelationId,
                PlayerExternalId = player.ExternalPlayerId,
                PlayerUsername = player.Username,
                GameExternalId = game.ExternalGameId,
                GameName = game.Name,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Type = transaction.Type,
                CreatedAtUtc = transaction.CreatedAtUtc
            };

            await publisher.PublishAsync(message, cancellationToken);
            logger.LogInformation("Queued transaction {ExternalTransactionId} with correlation {CorrelationId}.", transaction.ExternalTransactionId, transaction.CorrelationId);

            return MapAcceptedResponse(transaction);
        }

        throw new InvalidOperationException("Transaction creation exhausted retry attempts.");
    }

    private async Task<Transaction?> GetExistingTransactionAsync(string externalTransactionId, CancellationToken cancellationToken)
    {
        return await dbContext.Transactions
            .Include(x => x.Player)
            .Include(x => x.Game)
            .FirstOrDefaultAsync(x => x.ExternalTransactionId == externalTransactionId, cancellationToken);
    }

    private static CreateTransactionRequest Normalize(CreateTransactionRequest request)
    {
        return new CreateTransactionRequest
        {
            ExternalTransactionId = request.ExternalTransactionId.Trim(),
            PlayerExternalId = request.PlayerExternalId.Trim(),
            PlayerUsername = request.PlayerUsername.Trim(),
            Country = request.Country.Trim().ToUpperInvariant(),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            GameExternalId = request.GameExternalId.Trim(),
            GameName = request.GameName.Trim(),
            Provider = request.Provider.Trim(),
            Amount = request.Amount,
            Type = request.Type
        };
    }

    private static void EnsureMatchingRequest(Transaction existingTransaction, CreateTransactionRequest request)
    {
        var matches =
            string.Equals(existingTransaction.ExternalTransactionId, request.ExternalTransactionId, StringComparison.Ordinal) &&
            string.Equals(existingTransaction.Player?.ExternalPlayerId, request.PlayerExternalId, StringComparison.Ordinal) &&
            string.Equals(existingTransaction.Player?.Username, request.PlayerUsername, StringComparison.Ordinal) &&
            string.Equals(existingTransaction.Player?.Country, request.Country, StringComparison.Ordinal) &&
            string.Equals(existingTransaction.Player?.Currency, request.Currency, StringComparison.Ordinal) &&
            string.Equals(existingTransaction.Game?.ExternalGameId, request.GameExternalId, StringComparison.Ordinal) &&
            string.Equals(existingTransaction.Game?.Name, request.GameName, StringComparison.Ordinal) &&
            string.Equals(existingTransaction.Game?.Provider, request.Provider, StringComparison.Ordinal) &&
            string.Equals(existingTransaction.Currency, request.Currency, StringComparison.Ordinal) &&
            existingTransaction.Amount == request.Amount &&
            existingTransaction.Type == request.Type;

        if (matches)
        {
            return;
        }

        throw new DuplicateTransactionConflictException(
            $"Transaction ID '{request.ExternalTransactionId}' already exists with different payload values. Reuse the same payload for retries or send a new unique transaction ID.");
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
               postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static TransactionAcceptedResponse MapAcceptedResponse(Transaction transaction)
    {
        return new TransactionAcceptedResponse
        {
            TransactionId = transaction.Id,
            ExternalTransactionId = transaction.ExternalTransactionId,
            CorrelationId = transaction.CorrelationId,
            Status = transaction.Status,
            CreatedAtUtc = transaction.CreatedAtUtc
        };
    }
}
