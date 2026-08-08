namespace FinEventHub.Aggregation.Api.Entities;

public class ProcessedEvent
{
    public Guid EventId { get; set; }

    public DateTime ProcessedAtUtc { get; set; }
}