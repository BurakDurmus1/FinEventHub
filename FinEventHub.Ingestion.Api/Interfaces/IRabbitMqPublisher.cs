using FinEventHub.Contracts.Messages;

namespace FinEventHub.Ingestion.Api.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishAsync(EventMessage message, CancellationToken cancellationToken = default);
}