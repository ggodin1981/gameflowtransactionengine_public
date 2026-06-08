using GameFlow.Shared.Enums;

namespace GameFlow.Shared.Messaging;

public sealed class TransactionCommandMessage
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
    public TransactionType Type { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
