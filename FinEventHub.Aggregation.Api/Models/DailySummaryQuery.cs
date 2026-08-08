namespace FinEventHub.Aggregation.Api.Models;

public sealed class DailySummaryQuery
{
    public DateOnly Date { get; init; }

    public string Currency { get; init; } = string.Empty;
}