using FinEventHub.Aggregation.Api.Data;
using FinEventHub.Aggregation.Api.Entities;
using FinEventHub.Aggregation.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinEventHub.Aggregation.Api.Services;

public sealed class DailySummaryService : IDailySummaryService
{
    private readonly AppDbContext _context;

    public DailySummaryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DailySummary?> GetAsync(
        string customerId,
        DateOnly date,
        string currency,
        CancellationToken cancellationToken = default)
    {
        return await _context.DailySummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.CustomerId == customerId
                  && x.Date == date
                  && x.Currency == currency,
                cancellationToken);
    }
}