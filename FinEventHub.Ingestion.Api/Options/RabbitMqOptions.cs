namespace FinEventHub.Ingestion.Api.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = default!;

    public int Port { get; init; }

    public string Username { get; init; } = default!;

    public string Password { get; init; } = default!;

    public string QueueName { get; init; } = default!;
}