using EventGrok.Models;

namespace EventGrok.Services;

public class BookingProcessingBackgroundService(IBookingService bookingService, IEventService eventService) : BackgroundService
{
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                IEnumerable<Booking> bookings = await bookingService.GetPendingBookingsAsync();

                IEnumerable<Task> tasks = bookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        await Task.Delay(2000, stoppingToken);

        await _processingSemaphore.WaitAsync();
        try
        {
            Action<Booking> applyStatus;
            
            try
            {
                Event eventToBook = eventService.GetEventById(booking.EventId);
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
                    Event eventToBook = eventService.GetEventById(booking.EventId);
                    eventToBook.ReleaseSeats();
                }
                catch
                {
                    // Событие удалено между попытками - нечего освобождать
                }
                
                applyStatus = booking => booking.Reject();
            }

            applyStatus(booking);
            await bookingService.UpdateBookingAsync(booking);
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}