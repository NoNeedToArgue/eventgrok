namespace EventGrok.Events.Infrastructure.Settings;

public class KafkaSettings
{
    public required string BootstrapServers { get; set; }
    public required string ConsumerGroup { get; set; }
}
