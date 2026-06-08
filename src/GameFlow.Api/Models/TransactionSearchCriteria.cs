namespace GameFlow.Api.Models;

public sealed class TransactionSearchCriteria
{
    public string? Player { get; set; }
    public string? TransactionId { get; set; }
    public string? Game { get; set; }
    public string? Status { get; set; }
    public DateTime? DateFromUtc { get; set; }
    public DateTime? DateToUtc { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}
