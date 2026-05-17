using EventGrok.Models;
using Microsoft.EntityFrameworkCore;

namespace EventGrok.DataAccess.Repositories;

public class BookingRepository(AppDbContext context) : IBookingRepository
{
    public async Task AddBookingAsync(Booking booking, CancellationToken ct = default) =>
        await context.Bookings.AddAsync(booking, ct);

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default) =>
        await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);

    public async Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(CancellationToken ct = default) =>
        await context.Bookings.Where(b => b.Status == BookingStatus.Pending).ToListAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);
}