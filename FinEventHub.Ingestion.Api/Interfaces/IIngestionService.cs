using FinEventHub.Contracts.Requests;

namespace FinEventHub.Ingestion.Api.Interfaces;

public interface IIngestionService
{
    Task<int> ProcessBatchAsync(
        BatchEventRequest request,
        CancellationToken cancellationToken = default);
}