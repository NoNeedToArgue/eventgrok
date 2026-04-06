using EventGrok.Models;

namespace EventGrok.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId);

    Task<Booking> GetBookingByIdAsync(Guid bookingId);

    Task<IEnumerable<Booking>> GetPendingBookingsAsync();

    Task UpdateBookingAsync(Booking booking);
}
