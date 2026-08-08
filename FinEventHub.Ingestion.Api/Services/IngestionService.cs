using FinEventHub.Contracts.Requests;
using FinEventHub.Ingestion.Api.Interfaces;

namespace FinEventHub.Ingestion.Api.Services;

public sealed class IngestionService : IIngestionService
{
    private readonly IRabbitMqPublisher _publisher;

    public IngestionService(IRabbitMqPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<int> ProcessBatchAsync(
        BatchEventRequest request,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in request.Events)
        {
            await _publisher.PublishAsync(
                item,
                cancellationToken);
        }

        return request.Events.Count;
    }
}