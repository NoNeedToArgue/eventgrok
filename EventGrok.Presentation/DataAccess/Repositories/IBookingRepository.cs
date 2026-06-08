using EventGrok.Models;

namespace EventGrok.DataAccess.Repositories;

public interface IBookingRepository
{
    Task AddBookingAsync(Booking booking, CancellationToken ct = default);

    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);

    Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}