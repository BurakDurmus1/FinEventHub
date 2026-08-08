namespace FinEventHub.Aggregation.Api.Models;

public sealed class DailySummaryRequest
{
    public string CustomerId { get; init; } = string.Empty;

    public DateOnly Date { get; init; }

    public string Currency { get; init; } = string.Empty;
}