using EventGrok.Bookings.Application.DTOs;
using EventGrok.Bookings.Domain.Entities;

namespace EventGrok.Bookings.Application.Services;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default);

    Task<BookingDto> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);

    Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(CancellationToken ct = default);

    Task CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken ct = default);

    Task CommitChangesAsync(CancellationToken ct = default);
}
