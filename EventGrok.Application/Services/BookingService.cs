using EventGrok.Domain.Exceptions;
using EventGrok.Domain.Entities;
using EventGrok.Application.Interfaces;
using EventGrok.Application.DTOs;

namespace EventGrok.Application.Services;

public class BookingService(IBookingRepository bookingRepo, IEventRepository eventRepo) : IBookingService
{
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

    public async Task<BookingDto> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        await _bookingSemaphore.WaitAsync(ct);
        try
        {
            Event eventToBook = await eventRepo.GetEventByIdAsync(eventId, ct) ??
                throw new KeyNotFoundException($"Событие с id = {eventId} не найдено");

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
            throw new KeyNotFoundException($"Бронирование с id = {bookingId} не найдено");

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

    public async Task CommitChangesAsync(CancellationToken ct = default) =>
        await bookingRepo.SaveChangesAsync(ct);
}