using GameFlow.Shared.Enums;

namespace GameFlow.Shared.Entities;

public sealed class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public Guid PlayerId { get; set; }
    public Player? Player { get; set; }
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public ICollection<TransactionEvent> Events { get; set; } = new List<TransactionEvent>();
}
