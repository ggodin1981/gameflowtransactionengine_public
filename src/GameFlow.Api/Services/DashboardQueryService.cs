using GameFlow.Shared.Contracts.Dashboard;
using GameFlow.Shared.Enums;
using GameFlow.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Api.Services;

public sealed class DashboardQueryService(GameFlowDbContext dbContext) : IDashboardQueryService
{
    public async Task<DashboardOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var sinceUtc = DateTime.UtcNow.AddHours(-24);

        var totalTransactions24h = await dbContext.Transactions.CountAsync(x => x.CreatedAtUtc >= sinceUtc, cancellationToken);
        var settledAmount24h = await dbContext.Transactions
            .Where(x => x.CreatedAtUtc >= sinceUtc && x.Status == TransactionStatus.Completed)
            .Select(x => (decimal?)x.Amount)
            .SumAsync(cancellationToken) ?? 0m;
        var failedTransactions24h = await dbContext.Transactions.CountAsync(x => x.CreatedAtUtc >= sinceUtc && x.Status == TransactionStatus.Failed, cancellationToken);
        var activePlayers24h = await dbContext.Transactions
            .Where(x => x.CreatedAtUtc >= sinceUtc)
            .Select(x => x.PlayerId)
            .Distinct()
            .CountAsync(cancellationToken);
        var queueDepth = await dbContext.Transactions.CountAsync(
            x => x.CreatedAtUtc >= sinceUtc && (x.Status == TransactionStatus.Pending || x.Status == TransactionStatus.Processing),
            cancellationToken);

        var recentActivity = await dbContext.Transactions
            .AsNoTracking()
            .Include(x => x.Player)
            .Include(x => x.Game)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(8)
            .Select(x => new TransactionActivityDto
            {
                TransactionId = x.Id,
                ExternalTransactionId = x.ExternalTransactionId,
                PlayerUsername = x.Player!.Username,
                GameName = x.Game!.Name,
                Amount = x.Amount,
                Currency = x.Currency,
                Status = x.Status,
                OccurredAtUtc = x.ProcessedAtUtc ?? x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new DashboardOverviewResponse
        {
            TotalTransactions24h = totalTransactions24h,
            SettledAmount24h = settledAmount24h,
            FailedTransactions24h = failedTransactions24h,
            ActivePlayers24h = activePlayers24h,
            QueueDepth = queueDepth,
            ActiveConnections = 0,
            RecentActivity = recentActivity,
            ServiceHealth =
            [
                new ServiceHealthDto { Service = "api", Status = "healthy", Detail = "Accepting requests and exposing query endpoints." },
                new ServiceHealthDto { Service = "postgres", Status = "healthy", Detail = "Primary system of record for transactions and audits." },
                new ServiceHealthDto { Service = "worker", Status = "unknown", Detail = "Worker status is surfaced via logs and container health checks." }
            ]
        };
    }
}
