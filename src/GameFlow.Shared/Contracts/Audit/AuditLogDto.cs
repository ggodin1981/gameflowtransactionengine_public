namespace GameFlow.Shared.Contracts.Audit;

public sealed class AuditLogDto
{
    public Guid AuditLogId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string DetailsJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; }
}
