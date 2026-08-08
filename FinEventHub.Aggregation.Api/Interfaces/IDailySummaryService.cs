using FinEventHub.Aggregation.Api.Entities;

namespace FinEventHub.Aggregation.Api.Interfaces;

public interface IDailySummaryService
{
    Task<DailySummary?> GetAsync(
        string customerId,
        DateOnly date,
        string currency,
        CancellationToken cancellationToken = default);
}