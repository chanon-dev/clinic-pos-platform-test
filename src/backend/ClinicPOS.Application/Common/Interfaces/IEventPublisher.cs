namespace ClinicPOS.Application.Common.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(string exchange, string routingKey, object message, CancellationToken ct = default);
}
