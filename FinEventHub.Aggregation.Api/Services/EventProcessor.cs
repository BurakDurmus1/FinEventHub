using FinEventHub.Aggregation.Api.Data;
using FinEventHub.Aggregation.Api.Entities;
using FinEventHub.Aggregation.Api.Interfaces;
using FinEventHub.Contracts.Enums;
using FinEventHub.Contracts.Messages;
using Microsoft.EntityFrameworkCore;

namespace FinEventHub.Aggregation.Api.Services;

public sealed class EventProcessor : IEventProcessor
{
    private readonly AppDbContext _db;

    public EventProcessor(AppDbContext db)
    {
        _db = db;
    }

    public async Task ProcessAsync(
        EventMessage message,
        CancellationToken cancellationToken = default)
    {
        throw new Exception("Retry test");
        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);

        var alreadyProcessed =
            await _db.ProcessedEvents
                .AnyAsync(x => x.EventId == message.EventId, cancellationToken);

        if (alreadyProcessed)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        _db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = message.EventId,
            ProcessedAtUtc = DateTime.UtcNow
        });

        var date = DateOnly.FromDateTime(message.OccurredAt.UtcDateTime);

        var summary =
            await _db.DailySummaries.FirstOrDefaultAsync(x =>
                    x.CustomerId == message.CustomerId &&
                    x.Date == date &&
                    x.Currency == message.Currency,
                cancellationToken);

        if (summary is null)
        {
            summary = new DailySummary
            {
                CustomerId = message.CustomerId,
                Date = date,
                Currency = message.Currency
            };

            _db.DailySummaries.Add(summary);
        }

        if (message.Type == EventType.Credit)
            summary.TotalCredit += message.Amount;
        else
            summary.TotalDebit += message.Amount;

        summary.NetAmount =
            summary.TotalCredit - summary.TotalDebit;

        summary.UniqueEventCount++;

        summary.LastUpdatedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}