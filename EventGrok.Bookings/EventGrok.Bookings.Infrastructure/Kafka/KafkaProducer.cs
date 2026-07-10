using EventGrok.Bookings.Application.Interfaces;
using EventGrok.Bookings.Infrastructure.Settings;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventGrok.Bookings.Infrastructure.Kafka;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(KafkaSettings settings, ILogger<KafkaProducer> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync<T>(string topic, T message, string key, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message);
        var kafkaMessage = new Message<string, string>
        {
            Key = key,
            Value = json
        };

        try
        {
            var result = await _producer.ProduceAsync(topic, kafkaMessage, ct);
            _logger.LogInformation(
                "Published to {Topic} partition {Partition} offset {Offset} key={Key}",
                result.Topic, result.Partition.Value, result.Offset.Value, key);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish to {Topic} key={Key}", topic, key);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}