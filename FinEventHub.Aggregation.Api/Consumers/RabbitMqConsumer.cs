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
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());

            var message = JsonSerializer.Deserialize<EventMessage>(json);

            if (message is null)
            {
                await _channel!.BasicNackAsync(args.DeliveryTag, false, true);
                return;
            }

            _logger.LogInformation(
                "Received Event {EventId}",
                message.EventId);

            using var scope = _scopeFactory.CreateScope();

            var processor = scope.ServiceProvider
                .GetRequiredService<IEventProcessor>();

            await processor.ProcessAsync(message);

            await _channel.BasicAckAsync(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Consumer Error");

            await _channel!.BasicNackAsync(args.DeliveryTag, false, true);
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
}