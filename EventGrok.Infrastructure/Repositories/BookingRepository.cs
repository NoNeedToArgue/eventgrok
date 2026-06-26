using EventGrok.Domain.Entities;
using EventGrok.Infrastructure.Data;
using EventGrok.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventGrok.Infrastructure.Repositories;

public class BookingRepository(AppDbContext context) : IBookingRepository
{
    public async Task AddBookingAsync(Booking booking, CancellationToken ct = default) =>
        await context.Bookings.AddAsync(booking, ct);

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default) =>
        await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);

    public async Task<IReadOnlyList<Booking>> GetPendingBookingsAsync(CancellationToken ct = default) =>
        await context.Bookings.Where(b => b.Status == BookingStatus.Pending).ToListAsync(ct);

    public async Task<int> GetActiveBookingsCountByUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.Bookings.CountAsync(b => b.UserId == userId && b.Status != BookingStatus.Cancelled, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);
}