using FinEventHub.Contracts.Messages;

namespace FinEventHub.Contracts.Requests
{
    public sealed record BatchEventRequest
    {
        public IReadOnlyCollection<EventMessage> Events { get; init; }
       = [];
    }
}
