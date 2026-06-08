using GameFlow.Shared.Enums;

namespace GameFlow.Shared.Contracts.Transactions;

public sealed class TransactionSearchItem
{
    public Guid TransactionId { get; set; }
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string PlayerExternalId { get; set; } = string.Empty;
    public string PlayerUsername { get; set; } = string.Empty;
    public string GameExternalId { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? FailureReason { get; set; }
}
