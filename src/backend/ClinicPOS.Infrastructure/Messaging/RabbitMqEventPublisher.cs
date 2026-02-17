using System.Text;
using System.Text.Json;
using ClinicPOS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ClinicPOS.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private bool _initialized;

    public RabbitMqEventPublisher(IConnection connection, ILogger<RabbitMqEventPublisher> logger)
    {
        _connection = connection;
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _logger = logger;
    }

    public async Task PublishAsync(string exchange, string routingKey, object message, CancellationToken ct = default)
    {
        try
        {
            if (!_initialized)
            {
                await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
                _initialized = true;
            }

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel.BasicPublishAsync(exchange, routingKey, mandatory: false, basicProperties: props, body: body, cancellationToken: ct);
            _logger.LogInformation("Published event {RoutingKey} to {Exchange}", routingKey, exchange);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish event {RoutingKey}", routingKey);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _channel.DisposeAsync();
    }
}
