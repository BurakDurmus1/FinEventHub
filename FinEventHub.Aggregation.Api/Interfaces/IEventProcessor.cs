using FinEventHub.Contracts.Messages;

namespace FinEventHub.Aggregation.Api.Interfaces;

public interface IEventProcessor
{
    Task ProcessAsync(EventMessage message, CancellationToken cancellationToken = default);
}