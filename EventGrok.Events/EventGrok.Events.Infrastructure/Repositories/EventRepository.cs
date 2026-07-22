using EventGrok.Events.Domain.Entities;
using EventGrok.Events.Infrastructure.Data;
using EventGrok.Events.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace EventGrok.Events.Infrastructure.Repositories;

public class EventRepository(EventsDbContext context) : IEventRepository
{
    public async Task<(IReadOnlyList<Event> Events, int TotalCount)> GetEventsAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        IQueryable<Event> query = context.Events.AsNoTracking();

        if (title is not null)
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));

        if (from is not null)
            query = query.Where(e => e.StartAt >= from);

        if (to is not null)
            query = query.Where(e => e.EndAt <= to);

        int totalCount = await query.CountAsync(ct);

        List<Event> events = await query
            .OrderBy(e => e.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (events, totalCount);
    }

    public async Task<Event?> GetEventByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Event>> GetTopEventsAsync(int count, CancellationToken ct = default)
    {
        IQueryable<Event> query = context.Events
            .AsNoTracking()
            .Where(e => e.TotalSeats > 0)
            .OrderByDescending(e => (e.TotalSeats - e.AvailableSeats) / (double)e.TotalSeats)
            .Take(count);

        return await query.ToListAsync(ct);
    }

    public async Task AddEventAsync(Event newEvent, CancellationToken ct = default)
    {
        await context.Events.AddAsync(newEvent, ct);
    }

    public async Task RemoveEventAsync(Event existingEvent, CancellationToken ct = default)
    {
        context.Events.Remove(existingEvent);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);
}