using EventGrok.Bookings.Domain.Entities;
using EventGrok.Bookings.Application.Interfaces;
using EventGrok.Contracts.Events;
using EventGrok.Contracts.Topics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventGrok.Bookings.Application.BackgroundServices;

public class BookingProcessingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IKafkaProducer kafkaProducer,
    ILogger<BookingProcessingBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<Guid> pendingBookingIds;

                await using (var scope = scopeFactory.CreateAsyncScope())
                {
                    var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                    IEnumerable<Booking> bookings = await bookingRepo.GetPendingBookingsAsync(stoppingToken);
                    pendingBookingIds = [.. bookings.Select(b => b.Id)];
                }

                IEnumerable<Task> tasks = pendingBookingIds.Select(id => ProcessBookingAsync(id, stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        Booking booking = await bookingRepo.GetBookingByIdAsync(bookingId, stoppingToken) ??
            throw new KeyNotFoundException($"Booking with id = {bookingId} not found");

        Action<Booking> applyStatus;

        try
        {
            applyStatus = booking => booking.Confirm();
        }
        catch (KeyNotFoundException)
        {
            applyStatus = booking => booking.Reject();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            applyStatus = booking => booking.Reject();
        }

        applyStatus(booking);
        await bookingRepo.SaveChangesAsync(stoppingToken);

        if (booking.Status == BookingStatus.Confirmed)
        {
            BookingConfirmed bookingConfirmed = new()
            {
                BookingId = booking.Id,
                EventId = booking.EventId,
                UserId = booking.UserId,
                ConfirmedAt = DateTime.UtcNow
            };

            await kafkaProducer.ProduceAsync(
                TopicNames.BookingConfirmed,
                bookingConfirmed,
                booking.EventId.ToString(),
                stoppingToken);

            logger.LogInformation("Booking {BookingId} confirmed and published to Kafka", booking.Id);
        }
    }
}