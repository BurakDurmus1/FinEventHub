namespace FinEventHub.Aggregation.Api.Options;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = default!;
    public int Port { get; set; }

    public string Username { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string QueueName { get; set; } = default!;
    public string RetryQueueName { get; set; } = default!;

    public string DeadLetterQueueName { get; set; } = default!;

    public int RetryDelayMilliseconds { get; set; }

    public int MaxRetryCount { get; set; }
    public ushort PrefetchCount { get; set; } = 50;
    public int ConsumerConcurrency { get; set; } = 1;
}