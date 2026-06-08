using EventGrok.Domain.Entities;

namespace EventGrok.Application.Interfaces;

public interface IBookingRepository
{
    Task AddBookingAsync(Booking booking, CancellationToken ct = default);

    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);

    Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}