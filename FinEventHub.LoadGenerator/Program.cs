using Bogus;
using FinEventHub.Contracts.Enums;
using FinEventHub.Contracts.Messages;
using System.Diagnostics;
using System.Net.Http.Json;

const int totalEvents = 100_000;
const int batchSize = 100;
const int customerCount = 100;
const double duplicateRate = 0.10;

var client = new HttpClient
{
    BaseAddress = new Uri("https://localhost:44317"),
    Timeout = TimeSpan.FromMinutes(10)
};

var faker = new Faker();

var uniqueEvents = new List<EventMessage>();

for (var i = 0; i < totalEvents * (1 - duplicateRate); i++)
{
    uniqueEvents.Add(new EventMessage
    {
        EventId = Guid.NewGuid(),
        CustomerId = $"customer-{faker.Random.Int(1, customerCount):000}",
        Type = faker.PickRandom(EventType.Credit, EventType.Debit),
        Amount = faker.Random.Decimal(1, 5000),
        Currency = "TRY",
        OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-faker.Random.Int(0, 1440))
    });
}

var allEvents = new List<EventMessage>(uniqueEvents);

var duplicateCount = totalEvents - uniqueEvents.Count;

for (var i = 0; i < duplicateCount; i++)
{
    allEvents.Add(faker.PickRandom(uniqueEvents));
}

allEvents = faker.Random.Shuffle(allEvents).ToList();

var stopwatch = Stopwatch.StartNew();

var accepted = 0;
var failed = 0;

foreach (var batch in allEvents.Chunk(batchSize))
{
    var request = new
    {
        Events = batch.ToList()
    };

    var response = await client.PostAsJsonAsync(
        "/api/v1/events/batch",
        request);

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine(
            $"Batch failed. Status={(int)response.StatusCode}");
    }

    if (response.IsSuccessStatusCode)
        accepted += batch.Count();
    else
        failed += batch.Count();
}

stopwatch.Stop();

Console.WriteLine($"Total Sent           : {totalEvents}");
Console.WriteLine($"Accepted             : {accepted}");
Console.WriteLine($"Unique Events        : {uniqueEvents.Count}");
Console.WriteLine($"Duplicate Events     : {duplicateCount}");
Console.WriteLine($"Failed               : {failed}");
Console.WriteLine($"Elapsed              : {stopwatch.Elapsed}");