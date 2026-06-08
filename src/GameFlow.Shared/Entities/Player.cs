namespace GameFlow.Shared.Entities;

public sealed class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ExternalPlayerId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Country { get; set; } = "PH";
    public string Currency { get; set; } = "EUR";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
