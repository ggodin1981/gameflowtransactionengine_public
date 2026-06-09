using GameFlow.Api.Services;
using GameFlow.Shared.Contracts.Transactions;
using GameFlow.Shared.Enums;
using GameFlow.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameFlow.Api.Tests;

public sealed class TransactionCommandServiceTests
{
    [Fact]
    public async Task Reuses_existing_transaction_for_duplicate_submission()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var publisher = new RecordingPublisher();
        var request = CreateRequest();

        await using (var dbContext = CreateDbContext(dbName))
        {
            var service = CreateService(dbContext, publisher);
            var firstResponse = await service.CreateAsync(request, CancellationToken.None);
            var secondResponse = await service.CreateAsync(request, CancellationToken.None);

            Assert.Equal(firstResponse.TransactionId, secondResponse.TransactionId);
            Assert.Equal(firstResponse.ExternalTransactionId, secondResponse.ExternalTransactionId);
        }

        await using (var verificationContext = CreateDbContext(dbName))
        {
            Assert.Equal(1, await verificationContext.Transactions.CountAsync());
        }

        Assert.Equal(1, publisher.PublishCount);
    }

    [Fact]
    public async Task Rejects_same_transaction_id_with_different_payload()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var publisher = new RecordingPublisher();
        var originalRequest = CreateRequest();
        var conflictingRequest = CreateRequest();
        conflictingRequest.Amount = 250m;

        await using var dbContext = CreateDbContext(dbName);
        var service = CreateService(dbContext, publisher);
        await service.CreateAsync(originalRequest, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DuplicateTransactionConflictException>(
            () => service.CreateAsync(conflictingRequest, CancellationToken.None));

        Assert.Contains("already exists with different payload values", exception.Message);
        Assert.Equal(1, publisher.PublishCount);
    }

    private static TransactionCommandService CreateService(GameFlowDbContext dbContext, RecordingPublisher publisher)
    {
        return new TransactionCommandService(dbContext, publisher, NullLogger<TransactionCommandService>.Instance);
    }

    private static GameFlowDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<GameFlowDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new GameFlowDbContext(options);
    }

    private static CreateTransactionRequest CreateRequest()
    {
        return new CreateTransactionRequest
        {
            ExternalTransactionId = "TXN-ORDER-10001",
            PlayerExternalId = "PLY-1",
            PlayerUsername = "tester",
            Country = "PH",
            Currency = "EUR",
            GameExternalId = "GM-1",
            GameName = "Book of Nile",
            Provider = "EveryMatrix Studio",
            Amount = 100m,
            Type = TransactionType.Debit
        };
    }

    private sealed class RecordingPublisher : IRabbitMqPublisher
    {
        public int PublishCount { get; private set; }

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        {
            PublishCount++;
            return Task.CompletedTask;
        }
    }
}
