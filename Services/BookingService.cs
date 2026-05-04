using EventGrok.DataAccess;
using EventGrok.Exceptions;
using EventGrok.Models;
using Microsoft.EntityFrameworkCore;

namespace EventGrok.Services;

public class BookingService(AppDbContext context) : IBookingService
{
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        await _bookingSemaphore.WaitAsync();
        try
        {
            Event eventToBook = await context.Events.FindAsync(eventId) ??
                throw new KeyNotFoundException($"Событие с id = {eventId} не найдено");

            if (!eventToBook.TryReserveSeats(1))
                throw new NoAvailableSeatsException("Нет доступных мест на это событие");

            Booking booking = new()
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await context.AddAsync(booking);
            await context.SaveChangesAsync();

            return booking;
        }
        finally
        {
            _bookingSemaphore.Release();
        }
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        Booking booking = await context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId) ??
            throw new KeyNotFoundException($"Бронирование с id = {bookingId} не найдено");

        return booking;
    }

    public async Task<IEnumerable<Booking>> GetPendingBookingsAsync() =>
        await context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Pending)
            .ToListAsync();

    public async Task UpdateBookingAsync(Booking booking)
    {
        Booking existingBooking = await context.Bookings.FindAsync(booking.Id) ??
            throw new KeyNotFoundException($"Бронирование с id = {booking.Id} не найдено");

        existingBooking.Status = booking.Status;
        existingBooking.ProcessedAt = booking.ProcessedAt;

        await context.SaveChangesAsync();
    }
}