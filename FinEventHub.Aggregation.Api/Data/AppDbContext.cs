using FinEventHub.Aggregation.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinEventHub.Aggregation.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<DailySummary> DailySummaries => Set<DailySummary>();

    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProcessedEvent>()
            .HasKey(x => x.EventId);

        modelBuilder.Entity<DailySummary>()
            .HasIndex(x => new
            {
                x.CustomerId,
                x.Date,
                x.Currency
            })
            .IsUnique();
    }
}