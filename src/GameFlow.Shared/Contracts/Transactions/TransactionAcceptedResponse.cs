using GameFlow.Shared.Enums;

namespace GameFlow.Shared.Contracts.Transactions;

public sealed class TransactionAcceptedResponse
{
    public Guid TransactionId { get; set; }
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
