namespace GameFlow.Shared.Contracts.Players;

public sealed class PlayerProfileResponse
{
    public Guid PlayerId { get; set; }
    public string ExternalPlayerId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public int TotalTransactions { get; set; }
    public decimal LifetimeVolume { get; set; }
    public DateTime LastActivityUtc { get; set; }
}
