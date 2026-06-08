using GameFlow.Shared.Enums;

namespace GameFlow.Shared.Contracts.Dashboard;

public sealed class DashboardOverviewResponse
{
    public int TotalTransactions24h { get; set; }
    public decimal SettledAmount24h { get; set; }
    public int FailedTransactions24h { get; set; }
    public int ActivePlayers24h { get; set; }
    public int QueueDepth { get; set; }
    public int ActiveConnections { get; set; }
    public IReadOnlyList<TransactionActivityDto> RecentActivity { get; set; } = Array.Empty<TransactionActivityDto>();
    public IReadOnlyList<ServiceHealthDto> ServiceHealth { get; set; } = Array.Empty<ServiceHealthDto>();
}

public sealed class TransactionActivityDto
{
    public Guid TransactionId { get; set; }
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string PlayerUsername { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}

public sealed class ServiceHealthDto
{
    public string Service { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
