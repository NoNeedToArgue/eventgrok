using EventGrok.Models;

namespace EventGrok.Services;

public class BookingProcessingBackgroundService(IServiceScopeFactory scopeFactory) : BackgroundService
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
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                    IEnumerable<Booking> bookings = await bookingService.GetPendingBookingsAsync(stoppingToken);
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
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        Booking booking = await bookingService.GetBookingByIdAsync(bookingId, stoppingToken);

        Action<Booking> applyStatus;

        try
        {
            Event eventToBook = await eventService.GetEventByIdAsync(booking.EventId, stoppingToken);
            applyStatus = booking => booking.Confirm();
        }
        catch (KeyNotFoundException)
        {
            applyStatus = booking => booking.Reject();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            try
            {
                Event eventToBook = await eventService.GetEventByIdAsync(booking.EventId, stoppingToken);
                eventToBook.ReleaseSeats();
            }
            catch
            {
                // Событие удалено между попытками - нечего освобождать
            }

            applyStatus = booking => booking.Reject();
        }

        applyStatus(booking);
        await bookingService.CommitChangesAsync(stoppingToken);
    }
}