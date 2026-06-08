using System.Text.Json;
using GameFlow.Shared.Entities;
using GameFlow.Shared.Enums;
using GameFlow.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameFlow.Api.Services;

public static class DatabaseBootstrapper
{
    private static readonly string[] RequiredTables =
    [
        "Players",
        "Games",
        "Transactions",
        "TransactionEvents",
        "AuditLogs",
        "FailedMessages",
        "Cache"
    ];

    public static async Task InitializeAsync(GameFlowDbContext dbContext, ILogger logger, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await HasRequiredTablesAsync(dbContext, cancellationToken))
        {
            await SeedAsync(dbContext);
            return;
        }

        var existingTableCount = await CountTablesInCurrentSchemaAsync(dbContext, cancellationToken);
        if (existingTableCount > 0)
        {
            throw new InvalidOperationException(
                $"Database initialization found {existingTableCount} table(s) in the current schema, but the expected GameFlow tables were not all present.");
        }

        logger.LogWarning("EnsureCreated completed without materializing the expected GameFlow tables. Applying the generated create script explicitly.");

        var createScript = dbContext.Database.GenerateCreateScript();
        await dbContext.Database.ExecuteSqlRawAsync(createScript, cancellationToken);

        if (!await HasRequiredTablesAsync(dbContext, cancellationToken))
        {
            throw new InvalidOperationException("Generated create script ran, but the required GameFlow tables are still missing.");
        }

        await SeedAsync(dbContext);
    }

    public static async Task SeedAsync(GameFlowDbContext dbContext)
    {
        if (await dbContext.Games.AnyAsync())
        {
            return;
        }

        var player = new Player
        {
            ExternalPlayerId = "PLY-10001",
            Username = "slot_master",
            Country = "PH",
            Currency = "EUR"
        };

        var game = new Game
        {
            ExternalGameId = "GM-BOOK-001",
            Name = "Book of Nile",
            Provider = "EveryMatrix Studio"
        };

        var transaction = new Transaction
        {
            ExternalTransactionId = "TXN-SEED-0001",
            CorrelationId = "seed0001",
            Player = player,
            Game = game,
            Amount = 42.50m,
            Currency = "EUR",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-15),
            Status = TransactionStatus.Completed,
            Type = TransactionType.Debit,
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-14)
        };

        transaction.Events.Add(new TransactionEvent
        {
            EventType = "TransactionCompleted",
            Message = "Seed transaction used to hydrate the dashboard.",
            PayloadJson = JsonSerializer.Serialize(new { transaction.ExternalTransactionId }),
            OccurredAtUtc = transaction.ProcessedAtUtc ?? DateTime.UtcNow
        });

        dbContext.Games.Add(game);
        dbContext.Players.Add(player);
        dbContext.Transactions.Add(transaction);
        dbContext.AuditLogs.Add(new AuditLog
        {
            Action = "SeedDataCreated",
            Actor = "bootstrapper",
            EntityType = nameof(Transaction),
            EntityId = transaction.ExternalTransactionId,
            DetailsJson = JsonSerializer.Serialize(new { player.ExternalPlayerId, game.ExternalGameId, transaction.Amount })
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<bool> HasRequiredTablesAsync(GameFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var tables = new HashSet<string>(StringComparer.Ordinal);

        var connection = dbContext.Database.GetDbConnection();
        var openedHere = false;
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            openedHere = true;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = current_schema()
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        if (openedHere)
        {
            await dbContext.Database.CloseConnectionAsync();
        }

        return RequiredTables.All(tables.Contains);
    }

    private static async Task<int> CountTablesInCurrentSchemaAsync(GameFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var openedHere = false;
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            openedHere = true;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = current_schema()
            """;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (openedHere)
        {
            await dbContext.Database.CloseConnectionAsync();
        }

        return Convert.ToInt32(result);
    }
}
