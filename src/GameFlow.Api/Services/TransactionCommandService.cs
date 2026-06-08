using System.Text.Json;
using GameFlow.Shared.Contracts.Transactions;
using GameFlow.Shared.Entities;
using GameFlow.Shared.Enums;
using GameFlow.Shared.Messaging;
using GameFlow.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Api.Services;

public sealed class TransactionCommandService(
    GameFlowDbContext dbContext,
    IRabbitMqPublisher publisher,
    ILogger<TransactionCommandService> logger) : ITransactionCommandService
{
    public async Task<TransactionAcceptedResponse> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .FirstOrDefaultAsync(x => x.ExternalPlayerId == request.PlayerExternalId, cancellationToken)
            ?? new Player
            {
                ExternalPlayerId = request.PlayerExternalId,
                Username = request.PlayerUsername,
                Country = request.Country.ToUpperInvariant(),
                Currency = request.Currency.ToUpperInvariant(),
                CreatedAtUtc = DateTime.UtcNow
            };

        if (player.Id == Guid.Empty)
        {
            player.Id = Guid.NewGuid();
        }

        player.Username = request.PlayerUsername;
        player.Country = request.Country.ToUpperInvariant();
        player.Currency = request.Currency.ToUpperInvariant();

        if (dbContext.Entry(player).State == EntityState.Detached)
        {
            dbContext.Players.Add(player);
        }

        var game = await dbContext.Games
            .FirstOrDefaultAsync(x => x.ExternalGameId == request.GameExternalId, cancellationToken)
            ?? new Game
            {
                ExternalGameId = request.GameExternalId,
                Name = request.GameName,
                Provider = request.Provider,
                CreatedAtUtc = DateTime.UtcNow
            };

        game.Name = request.GameName;
        game.Provider = request.Provider;

        if (dbContext.Entry(game).State == EntityState.Detached)
        {
            dbContext.Games.Add(game);
        }

        var createdAtUtc = DateTime.UtcNow;
        var transaction = new Transaction
        {
            ExternalTransactionId = $"TXN-{createdAtUtc:yyyyMMddHHmmssfff}-{Random.Shared.Next(1000, 9999)}",
            CorrelationId = Guid.NewGuid().ToString("N"),
            Player = player,
            Game = game,
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            Status = TransactionStatus.Pending,
            Type = request.Type,
            CreatedAtUtc = createdAtUtc
        };

        transaction.Events.Add(new TransactionEvent
        {
            EventType = "TransactionCreated",
            Message = "Transaction accepted by API and queued for processing.",
            PayloadJson = JsonSerializer.Serialize(request),
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
                request.PlayerExternalId,
                request.GameExternalId,
                request.Amount,
                request.Type
            }),
            CreatedAtUtc = createdAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);

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
