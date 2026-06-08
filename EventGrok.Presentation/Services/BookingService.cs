using EventGrok.DataAccess.Repositories;
using EventGrok.Exceptions;
using EventGrok.Models;

namespace EventGrok.Services;

public class BookingService(IBookingRepository bookingRepo, IEventRepository eventRepo) : IBookingService
{
    private static readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
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

            return booking;
        }
        finally
        {
            _bookingSemaphore.Release();
        }
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default) =>
        await bookingRepo.GetBookingByIdAsync(bookingId, ct) ??
            throw new KeyNotFoundException($"Бронирование с id = {bookingId} не найдено");

    public async Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(CancellationToken ct = default) =>
        await bookingRepo.GetPendingBookingsAsync(ct);

    public async Task CommitChangesAsync(CancellationToken ct = default) =>
        await bookingRepo.SaveChangesAsync(ct);
}