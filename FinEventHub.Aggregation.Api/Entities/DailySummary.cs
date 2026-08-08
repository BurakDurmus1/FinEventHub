namespace FinEventHub.Aggregation.Api.Entities;

public class DailySummary
{
    public Guid Id { get; set; }

    public string CustomerId { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public string Currency { get; set; } = string.Empty;

    public decimal TotalCredit { get; set; }

    public decimal TotalDebit { get; set; }

    public decimal NetAmount { get; set; }

    public int UniqueEventCount { get; set; }

    public DateTime LastUpdatedUtc { get; set; }
}