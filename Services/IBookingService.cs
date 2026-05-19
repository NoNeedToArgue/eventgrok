using EventGrok.Models;

namespace EventGrok.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default);

    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);

    Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(CancellationToken ct = default);

    Task CommitChangesAsync(CancellationToken ct = default);
}
