namespace GameFlow.Shared.Entities;

public sealed class CacheEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CacheKey { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
}
