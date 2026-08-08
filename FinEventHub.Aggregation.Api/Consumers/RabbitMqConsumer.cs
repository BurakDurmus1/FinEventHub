using System.Text;
using System.Text.Json;
using FinEventHub.Aggregation.Api.Data;
using FinEventHub.Aggregation.Api.Interfaces;
using FinEventHub.Aggregation.Api.Options;
using FinEventHub.Contracts.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FinEventHub.Aggregation.Api.Consumers;

public sealed class RabbitMqConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);

        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        const string exchangeName = "events.exchange";

        await _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: stoppingToken);

        // Main Queue
        await _channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Retry Queue
        await _channel.QueueDeclareAsync(
            queue: _options.RetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = _options.RetryDelayMilliseconds,
                ["x-dead-letter-exchange"] = exchangeName,
                ["x-dead-letter-routing-key"] = _options.QueueName
            },
            cancellationToken: stoppingToken);

        // Dead Letter Queue
        await _channel.QueueDeclareAsync(
            queue: _options.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            _options.QueueName,
            exchangeName,
            _options.QueueName,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            _options.RetryQueueName,
            exchangeName,
            _options.RetryQueueName,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            _options.DeadLetterQueueName,
            exchangeName,
            _options.DeadLetterQueueName,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += OnMessageReceived;

        await _channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("RabbitMQ Consumer started.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs args)
    {
        const string RetryCountHeader = "x-retry-count";

        var retryCount = 0;

        if (args.BasicProperties.Headers is not null &&
            args.BasicProperties.Headers.TryGetValue(RetryCountHeader, out var value))
        {
            if (value is byte[] bytes &&
                int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed))
            {
                retryCount = parsed;
            }
        }

        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());

            EventMessage? message;

            try
            {
                message = JsonSerializer.Deserialize<EventMessage>(json);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "Failed to deserialize RabbitMQ message.");

                await MoveToDeadLetterAsync(args);

                return;
            }

            if (message is null)
            {
                _logger.LogWarning(
                    "Received null message after deserialization.");

                await MoveToDeadLetterAsync(args);

                return;
            }

            _logger.LogInformation(
                "Processing Event {EventId}",
                message.EventId);

            using var scope = _scopeFactory.CreateScope();

            var processor = scope.ServiceProvider
                .GetRequiredService<IEventProcessor>();

            await processor.ProcessAsync(message);

            await _channel!.BasicAckAsync(args.DeliveryTag, false);

            _logger.LogInformation(
                "Event {EventId} processed successfully.",
                message.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while processing RabbitMQ message. Retry={RetryCount}",
                retryCount);

            if (retryCount >= _options.MaxRetryCount)
            {
                _logger.LogWarning(
                    "Retry limit reached. Moving message to DLQ.");

                await MoveToDeadLetterAsync(args);

                return;
            }

            var properties = new BasicProperties
            {
                Persistent = true,
                Headers = CreateRetryHeaders(
                    args.BasicProperties.Headers,
                    retryCount)
            };

            await _channel!.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _options.RetryQueueName,
                mandatory: true,
                basicProperties: properties,
                body: args.Body);

            await _channel.BasicAckAsync(args.DeliveryTag, false);

            _logger.LogInformation(
                "Message sent to Retry Queue. Retry={RetryCount}",
                retryCount + 1);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();

        await base.StopAsync(cancellationToken);
    }
    private static IDictionary<string, object?> CreateRetryHeaders(
    IDictionary<string, object?>? existingHeaders,
    int retryCount)
    {
        var headers = existingHeaders is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(existingHeaders);

        headers["x-retry-count"] =
            Encoding.UTF8.GetBytes((retryCount + 1).ToString());

        return headers;
    }
    private async Task MoveToDeadLetterAsync(BasicDeliverEventArgs args)
    {
        _logger.LogWarning("Moving message to Dead Letter Queue.");

        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = args.BasicProperties.Headers
        };

        await _channel!.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _options.DeadLetterQueueName,
            mandatory: true,
            basicProperties: properties,
            body: args.Body);

        _logger.LogInformation("Message published to Dead Letter Queue.");

        await _channel.BasicAckAsync(args.DeliveryTag, false);

        _logger.LogInformation("Original message acknowledged.");
    }
}