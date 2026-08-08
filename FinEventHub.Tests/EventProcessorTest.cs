using FinEventHub.Aggregation.Api.Data;
using FinEventHub.Aggregation.Api.Services;
using FinEventHub.Contracts.Enums;
using FinEventHub.Contracts.Messages;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinEventHub.Tests;

public sealed class EventProcessorTests
{
    [Fact]
    public async Task ProcessAsync_Should_Not_Process_Duplicate_Event()
    {

        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var processor = new EventProcessor(db);

        var message = new EventMessage
        {
            EventId = Guid.NewGuid(),
            CustomerId = "customer-001",
            Type = EventType.Credit,
            Amount = 100,
            Currency = "TRY",
            OccurredAt = DateTimeOffset.UtcNow
        };

        await processor.ProcessAsync(message);

        await processor.ProcessAsync(message);

        db.ProcessedEvents.Should().HaveCount(1);

        db.DailySummaries.Should().HaveCount(1);

        db.DailySummaries.Single().UniqueEventCount.Should().Be(1);

        db.DailySummaries.Single().TotalCredit.Should().Be(100);

        db.DailySummaries.Single().NetAmount.Should().Be(100);
    }

    [Fact]
    public async Task ProcessAsync_Should_Create_DailySummary_For_Credit_Event()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var processor = new EventProcessor(db);

        var message = new EventMessage
        {
            EventId = Guid.NewGuid(),
            CustomerId = "customer-001",
            Type = EventType.Credit,
            Amount = 250,
            Currency = "TRY",
            OccurredAt = DateTimeOffset.UtcNow
        };

        await processor.ProcessAsync(message);

        var summary = await db.DailySummaries.SingleAsync();

        summary.CustomerId.Should().Be("customer-001");
        summary.Currency.Should().Be("TRY");
        summary.TotalCredit.Should().Be(250);
        summary.TotalDebit.Should().Be(0);
        summary.NetAmount.Should().Be(250);
        summary.UniqueEventCount.Should().Be(1);
    }
    [Fact]
    public async Task ProcessAsync_Should_Create_DailySummary_For_Debit_Event()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var processor = new EventProcessor(db);

        var message = new EventMessage
        {
            EventId = Guid.NewGuid(),
            CustomerId = "customer-001",
            Type = EventType.Debit,
            Amount = 150,
            Currency = "TRY",
            OccurredAt = DateTimeOffset.UtcNow
        };

        await processor.ProcessAsync(message);

        var summary = await db.DailySummaries.SingleAsync();

        summary.CustomerId.Should().Be("customer-001");
        summary.Currency.Should().Be("TRY");
        summary.TotalCredit.Should().Be(0);
        summary.TotalDebit.Should().Be(150);
        summary.NetAmount.Should().Be(-150);
        summary.UniqueEventCount.Should().Be(1);
    }
    [Fact]
    public async Task ProcessAsync_Should_Update_Existing_DailySummary()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var processor = new EventProcessor(db);

        var firstEvent = new EventMessage
        {
            EventId = Guid.NewGuid(),
            CustomerId = "customer-001",
            Type = EventType.Credit,
            Amount = 100,
            Currency = "TRY",
            OccurredAt = DateTimeOffset.UtcNow
        };

        var secondEvent = new EventMessage
        {
            EventId = Guid.NewGuid(),
            CustomerId = "customer-001",
            Type = EventType.Debit,
            Amount = 40,
            Currency = "TRY",
            OccurredAt = firstEvent.OccurredAt
        };

        await processor.ProcessAsync(firstEvent);
        await processor.ProcessAsync(secondEvent);

        db.DailySummaries.Should().HaveCount(1);

        var summary = await db.DailySummaries.SingleAsync();

        summary.TotalCredit.Should().Be(100);
        summary.TotalDebit.Should().Be(40);
        summary.NetAmount.Should().Be(60);
        summary.UniqueEventCount.Should().Be(2);
    }

    [Fact]
    public async Task ProcessAsync_Should_Create_Separate_DailySummaries_For_Different_Customers()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var processor = new EventProcessor(db);

        var firstEvent = new EventMessage
        {
            EventId = Guid.NewGuid(),
            CustomerId = "customer-001",
            Type = EventType.Credit,
            Amount = 100,
            Currency = "TRY",
            OccurredAt = DateTimeOffset.UtcNow
        };

        var secondEvent = new EventMessage
        {
            EventId = Guid.NewGuid(),
            CustomerId = "customer-002",
            Type = EventType.Credit,
            Amount = 250,
            Currency = "TRY",
            OccurredAt = firstEvent.OccurredAt
        };

        await processor.ProcessAsync(firstEvent);
        await processor.ProcessAsync(secondEvent);

        db.DailySummaries.Should().HaveCount(2);

        db.DailySummaries
            .Single(x => x.CustomerId == "customer-001")
            .TotalCredit
            .Should()
            .Be(100);

        db.DailySummaries
            .Single(x => x.CustomerId == "customer-002")
            .TotalCredit
            .Should()
            .Be(250);
    }
}