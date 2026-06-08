using GameFlow.Shared.Enums;

namespace GameFlow.Shared.Messaging;

public sealed class TransactionLifecycleEvent
{
    public Guid TransactionId { get; set; }
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PlayerUsername { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
