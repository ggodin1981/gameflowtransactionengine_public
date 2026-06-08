namespace GameFlow.Shared.Entities;

public sealed class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ExternalGameId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
