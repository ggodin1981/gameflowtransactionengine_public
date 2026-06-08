namespace GameFlow.Shared.Entities;

public sealed class TransactionEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
