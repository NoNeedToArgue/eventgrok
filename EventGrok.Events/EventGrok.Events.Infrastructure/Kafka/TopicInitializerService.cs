using EventGrok.Events.Infrastructure.Settings;
using EventGrok.Contracts.Topics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace EventGrok.Events.Infrastructure.Kafka;

public class TopicInitializerService(KafkaSettings settings, ILogger<TopicInitializerService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = settings.BootstrapServers
            };

            using var adminClient = new AdminClientBuilder(config).Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicExists = metadata.Topics.Any(t => t.Topic == TopicNames.BookingConfirmed);

            if (!topicExists)
            {
                var spec = new TopicSpecification
                {
                    Name = TopicNames.BookingConfirmed,
                    NumPartitions = 3,
                    ReplicationFactor = 1
                };

                await adminClient.CreateTopicsAsync([spec]);
                logger.LogInformation("Topic {Topic} created", TopicNames.BookingConfirmed);
            }
            else
            {
                logger.LogInformation("Topic {Topic} already exists", TopicNames.BookingConfirmed);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize topic {Topic}", TopicNames.BookingConfirmed);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
