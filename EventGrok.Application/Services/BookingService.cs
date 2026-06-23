using EventGrok.Domain.Exceptions;
using EventGrok.Domain.Entities;
using EventGrok.Application.Interfaces;
using EventGrok.Application.DTOs;

namespace EventGrok.Application.Services;

public class BookingService(IBookingRepository bookingRepo, IEventRepository eventRepo) : IBookingService
{
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

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
            if (activeBookingsCount >= 10)
                throw new ActiveBookingsLimitException("Превышен лимит активных бронирований (10)");

            if (!eventToBook.TryReserveSeats(1))
                throw new NoAvailableSeatsException("Нет доступных мест на это событие");

            Booking booking = new()
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
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
            throw new KeyNotFoundException($"Бронирование с id = {bookingId} не найдено");

        if (!isAdmin && booking.UserId != userId)
            throw new ForbiddenException("Можно отменять только свои бронирования");

        booking.Cancel();

        await bookingRepo.SaveChangesAsync(ct);
    }

    public async Task CommitChangesAsync(CancellationToken ct = default) =>
        await bookingRepo.SaveChangesAsync(ct);
}