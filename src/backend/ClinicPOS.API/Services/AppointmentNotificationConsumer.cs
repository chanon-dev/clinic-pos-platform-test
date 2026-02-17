using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ClinicPOS.API.Services;

public class AppointmentNotificationConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<AppointmentNotificationConsumer> _logger;
    private IChannel? _channel;
    private const string ExchangeName = "clinic-pos.events";
    private const string QueueName = "clinic-pos.notifications";
    private const string RoutingKey = "appointment.created";

    public AppointmentNotificationConsumer(IConnection connection, ILogger<AppointmentNotificationConsumer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
            await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var message = JsonSerializer.Deserialize<JsonElement>(body);

                    var tenantId = message.TryGetProperty("payload", out var payload)
                        && payload.TryGetProperty("tenantId", out var tid)
                        ? tid.GetString() : "unknown";

                    var appointmentId = payload.TryGetProperty("appointmentId", out var aid)
                        ? aid.GetString() : "unknown";

                    _logger.LogInformation(
                        "Processing appointment notification: AppointmentId={AppointmentId}, TenantId={TenantId}",
                        appointmentId, tenantId);

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing appointment notification");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            _logger.LogInformation("Appointment notification consumer started, listening on queue: {Queue}", QueueName);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Appointment notification consumer failed");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
