using EventGrok.DataAccess;
using EventGrok.Models;
using Microsoft.EntityFrameworkCore;

namespace EventGrok.Services;

public class EventService(AppDbContext context) : IEventService
{
    public async Task<PaginatedResultDto<Event>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        IQueryable<Event> query = context.Events.AsNoTracking();

        if (title is not null)
            query = query.Where(e => EF.Functions.ILike(e.Title, $"%{title}%"));

        if (from is not null)
            query = query.Where(e => e.StartAt >= from);

        if (to is not null)
            query = query.Where(e => e.EndAt <= to);

        int totalCount = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        List<Event> events = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResultDto<Event>(events, totalCount, page, pageSize, totalPages);
    }

    public async Task<Event> GetEventByIdAsync(Guid id)
    {
        Event eventById = await context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id) ??
            throw new KeyNotFoundException($"Событие с id = {id} не найдено");

        return eventById;
    }

    public async Task<Event> AddEventAsync(Event newEvent)
    {
        if (newEvent.EndAt <= newEvent.StartAt)
            throw new ArgumentException("Дата окончания события должна быть позже даты начала");

        await context.Events.AddAsync(newEvent);
        await context.SaveChangesAsync();

        return newEvent;
    }

    public async Task UpdateEventAsync(Guid id, Event updatedEvent)
    {
        Event existingEvent = await context.Events.FindAsync(id) ??
            throw new KeyNotFoundException($"Событие с id = {id} не найдено");

        if (updatedEvent.EndAt <= updatedEvent.StartAt)
            throw new ArgumentException("Дата окончания события должна быть позже даты начала");

        existingEvent.Title = updatedEvent.Title;
        existingEvent.Description = updatedEvent.Description;
        existingEvent.StartAt = updatedEvent.StartAt;
        existingEvent.EndAt = updatedEvent.EndAt;
        existingEvent.TotalSeats = updatedEvent.TotalSeats;

        await context.SaveChangesAsync();
    }

    public async Task RemoveEventAsync(Guid id)
    {
        Event existingEvent = await context.Events.FindAsync(id) ??
            throw new KeyNotFoundException($"Событие с id = {id} не найдено");

        context.Events.Remove(existingEvent);

        await context.SaveChangesAsync();
    }
}