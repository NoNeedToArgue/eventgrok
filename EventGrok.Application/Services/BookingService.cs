using EventGrok.Domain.Exceptions;
using EventGrok.Domain.Entities;
using EventGrok.Application.Interfaces;
using EventGrok.Application.DTOs;

namespace EventGrok.Application.Services;

public class BookingService(IBookingRepository bookingRepo, IEventRepository eventRepo) : IBookingService
{
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);
    public const int ActiveBookingsLimit = 10;

    public async Task<BookingDto> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        await _bookingSemaphore.WaitAsync(ct);
        try
        {
            Event eventToBook = await eventRepo.GetEventByIdAsync(eventId, ct) ??
                throw new EventNotFoundException(eventId);

            if (eventToBook.StartAt <= DateTime.UtcNow)
                throw new BookingPastEventException("Нельзя бронировать прошедшее событие");

            int activeBookingsCount = await bookingRepo.GetActiveBookingsCountByUserAsync(userId, ct);
            if (activeBookingsCount >= ActiveBookingsLimit)
                throw new ActiveBookingsLimitException($"Превышен лимит активных бронирований ({ActiveBookingsLimit})");

            if (!eventToBook.TryReserveSeats(1))
                throw new NoAvailableSeatsException("Нет доступных мест на это событие");

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
            throw new ForbiddenException("Можно отменять только свои бронирования");

        if (booking.Status == BookingStatus.Cancelled)
            throw new BookingAlreadyCancelledException();

        Event eventToRelease = await eventRepo.GetEventByIdAsync(booking.EventId, ct) ??
            throw new EventNotFoundException(booking.EventId);

        booking.Cancel();

        eventToRelease.ReleaseSeats(1);

        await bookingRepo.SaveChangesAsync(ct);
    }

    public async Task CommitChangesAsync(CancellationToken ct = default) =>
        await bookingRepo.SaveChangesAsync(ct);
}