using EventGrok.Bookings.Domain.Entities;

namespace EventGrok.Bookings.Application.Interfaces;

public interface IBookingRepository
{
    Task AddBookingAsync(Booking booking, CancellationToken ct = default);

    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);

    Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(CancellationToken ct = default);

    Task<int> GetActiveBookingsCountByUserAsync(Guid userId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}