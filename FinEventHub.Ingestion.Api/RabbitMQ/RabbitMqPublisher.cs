using FinEventHub.Contracts.Messages;
using FinEventHub.Ingestion.Api.Interfaces;

namespace FinEventHub.Ingestion.Api.RabbitMQ;

public sealed class RabbitMqPublisher : IRabbitMqPublisher
{
    public Task PublishAsync(
        EventMessage message,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}