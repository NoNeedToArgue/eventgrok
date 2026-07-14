namespace EventGrok.Bookings.Application.Interfaces;

public interface IKafkaProducer
{
    Task ProduceAsync<T>(string topic, T message, string key, CancellationToken ct = default);
}
