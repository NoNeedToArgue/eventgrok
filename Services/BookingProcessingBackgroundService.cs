using EventGrok.Models;

namespace EventGrok.Services;

public class BookingProcessingBackgroundService(IBookingService bookingService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                IEnumerable<Booking> bookings = await bookingService.GetPendingBookingsAsync();

                foreach (Booking booking in bookings)
                {
                    await Task.Delay(2000, stoppingToken);
                    
                    booking.Status = BookingStatus.Confirmed;
                    booking.ProcessedAt = DateTime.UtcNow;

                    await bookingService.UpdateBookingAsync(booking);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}