using EventGrok.Application.DTOs;
using EventGrok.Domain.Entities;

namespace EventGrok.Application.Services;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(Guid eventId, CancellationToken ct = default);

    Task<BookingDto> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);

    Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(CancellationToken ct = default);

    Task CommitChangesAsync(CancellationToken ct = default);
}
