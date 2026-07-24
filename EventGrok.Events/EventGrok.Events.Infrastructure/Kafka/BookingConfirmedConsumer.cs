using EventGrok.Events.Infrastructure.Settings;
using EventGrok.Contracts.Topics;
using EventGrok.Contracts.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Confluent.Kafka;
using System.Text.Json;
using EventGrok.Events.Application.Interfaces;
using EventGrok.Events.Application.Cache;
using EventGrok.Events.Domain.Entities;

namespace EventGrok.Events.Infrastructure.Kafka;

public class BookingConfirmedConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopeFactory,
    ICacheService cache,
    ILogger<BookingConfirmedConsumer> logger
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeMessagesAsync(stoppingToken), stoppingToken);
    }

    private async Task ConsumeMessagesAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(TopicNames.BookingConfirmed);

        logger.LogInformation("Consumer subscribed to {Topic}", TopicNames.BookingConfirmed);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    await ProcessMessageAsync(result.Message, stoppingToken);
                    consumer.Commit(result);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Kafka consume error");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(Message<string, string> message, CancellationToken ct)
    {
        var bookingConfirmed = JsonSerializer.Deserialize<BookingConfirmed>(message.Value)!;

        using var scope = scopeFactory.CreateScope();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        try
        {
            Event? eventEntity = await eventRepo.GetEventByIdAsync(bookingConfirmed.EventId, ct);

            if (eventEntity is null)
            {
                logger.LogWarning("Event {EventId} not found, skipping message", bookingConfirmed.EventId);
                return;
            }

            if (!eventEntity.TryReserveSeats(bookingConfirmed.SeatsCount))
            {
                logger.LogWarning(
                    "Not enough seats for event {EventId} (requested {SeatsCount}), skipping message",
                    bookingConfirmed.EventId, bookingConfirmed.SeatsCount);
                return;
            }

            await eventRepo.SaveChangesAsync(ct);

            await cache.RemoveAsync(CacheKeys.EventById(bookingConfirmed.EventId), ct);

            logger.LogInformation(
                "Reserved {SeatsCount} seats for event {EventId} (booking {BookingId})",
                bookingConfirmed.SeatsCount, bookingConfirmed.EventId, bookingConfirmed.BookingId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex,
                "Failed to process booking {BookingId} for event {EventId}, skipping message",
                bookingConfirmed.BookingId, bookingConfirmed.EventId);
        }
    }
}
