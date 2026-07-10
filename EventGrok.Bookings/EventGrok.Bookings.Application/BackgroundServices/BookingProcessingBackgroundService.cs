using EventGrok.Bookings.Domain.Entities;
using EventGrok.Bookings.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventGrok.Bookings.Application.BackgroundServices;

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
            throw new KeyNotFoundException($"Бронирование с id = {bookingId} не найдено");

        Action<Booking> applyStatus;

        try
        {
            // Event eventToBook = await eventRepo.GetEventByIdAsync(booking.EventId, stoppingToken) ??
            //     throw new KeyNotFoundException($"Событие с id = {booking.EventId} не найдено");
            applyStatus = booking => booking.Confirm();
        }
        catch (KeyNotFoundException)
        {
            applyStatus = booking => booking.Reject();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // try
            // {
            //     Event eventToBook = await eventRepo.GetEventByIdAsync(booking.EventId, stoppingToken) ??
            //         throw new KeyNotFoundException($"Событие с id = {booking.EventId} не найдено");
            //     eventToBook.ReleaseSeats();
            // }
            // catch
            // {
            //     // Событие удалено между попытками - нечего освобождать
            // }

            applyStatus = booking => booking.Reject();
        }

        applyStatus(booking);
        await bookingRepo.SaveChangesAsync(stoppingToken);
    }
}