using GameFlow.Shared.Enums;

namespace GameFlow.Shared.Contracts.Transactions;

public sealed class CreateTransactionRequest
{
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string PlayerExternalId { get; set; } = string.Empty;
    public string PlayerUsername { get; set; } = string.Empty;
    public string Country { get; set; } = "PH";
    public string Currency { get; set; } = "EUR";
    public string GameExternalId { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
}
