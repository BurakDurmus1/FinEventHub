using FinEventHub.Contracts.Enums;

namespace FinEventHub.Contracts.Messages
{
    public sealed record EventMessage
    {
        public Guid EventId { get; init; }

        public string CustomerId { get; init; } = default!;

        public EventType Type { get; init; }

        public decimal Amount { get; init; }

        public string Currency { get; init; } = default!;

        public DateTimeOffset OccurredAt { get; init; }
    }
}
