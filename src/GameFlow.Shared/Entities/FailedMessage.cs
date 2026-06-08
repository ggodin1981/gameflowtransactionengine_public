namespace GameFlow.Shared.Entities;

public sealed class FailedMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime FirstFailedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastFailedAtUtc { get; set; }
}
