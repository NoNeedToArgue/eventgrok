using EventGrok.Bookings.Domain.Exceptions;
using EventGrok.Bookings.Domain.Entities;
using EventGrok.Bookings.Application.Interfaces;
using EventGrok.Bookings.Application.DTOs;

namespace EventGrok.Bookings.Application.Services;

public class BookingService(IBookingRepository bookingRepo) : IBookingService
{
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);
    public const int ActiveBookingsLimit = 10;

    public async Task<BookingDto> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        await _bookingSemaphore.WaitAsync(ct);
        try
        {
            int activeBookingsCount = await bookingRepo.GetActiveBookingsCountByUserAsync(userId, ct);
            if (activeBookingsCount >= ActiveBookingsLimit)
                throw new ActiveBookingsLimitException($"Превышен лимит активных бронирований ({ActiveBookingsLimit})");

            Booking booking = new()
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                UserId = userId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await bookingRepo.AddBookingAsync(booking, ct);
            await bookingRepo.SaveChangesAsync(ct);

            return new BookingDto(
                booking.Id,
                booking.EventId,
                booking.Status.ToString(),
                booking.CreatedAt,
                booking.ProcessedAt
            );
        }
        finally
        {
            _bookingSemaphore.Release();
        }
    }

    public async Task<BookingDto> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        Booking booking = await bookingRepo.GetBookingByIdAsync(bookingId, ct) ??
            throw new BookingNotFoundException(bookingId);

        return new BookingDto(
            booking.Id,
            booking.EventId,
            booking.Status.ToString(),
            booking.CreatedAt,
            booking.ProcessedAt
        );
    }

    public async Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(CancellationToken ct = default) =>
        await bookingRepo.GetPendingBookingsAsync(ct);

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        Booking booking = await bookingRepo.GetBookingByIdAsync(bookingId, ct) ??
            throw new BookingNotFoundException(bookingId);

        if (!isAdmin && booking.UserId != userId)
            throw new NotOwnBookingException();

        if (booking.Status == BookingStatus.Cancelled)
            throw new BookingAlreadyCancelledException();

        booking.Cancel();

        await bookingRepo.SaveChangesAsync(ct);
    }

    public async Task CommitChangesAsync(CancellationToken ct = default) =>
        await bookingRepo.SaveChangesAsync(ct);
}