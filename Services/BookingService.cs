using System.Collections.Concurrent;
using EventGrok.Exceptions;
using EventGrok.Models;

namespace EventGrok.Services;

public class BookingService(IEventService eventService) : IBookingService
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = [];
    private readonly object _bookingLock = new();

    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        Booking booking;

        lock (_bookingLock)
        {
            Event eventToBook = eventService.GetEventById(eventId);

            if (!eventToBook.TryReserveSeats(1))
                throw new NoAvailableSeatsException("Нет доступных мест на это событие");

            booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _bookings[booking.Id] = booking;
        }

        return booking;
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        if (!_bookings.ContainsKey(bookingId))
            throw new KeyNotFoundException($"Бронирование с id = {bookingId} не найдено");

        return _bookings[bookingId];
    }

    public async Task<IEnumerable<Booking>> GetPendingBookingsAsync() =>
        [.. _bookings.Values.Where(b => b.Status == BookingStatus.Pending)];

    public async Task UpdateBookingAsync(Booking booking)
    {
        _bookings[booking.Id] = booking;
    }
}