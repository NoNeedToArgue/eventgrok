using EventGrok.DataAccess.Repositories;
using EventGrok.Models;

namespace EventGrok.Services;

public class EventService(IEventRepository eventRepo) : IEventService
{
    public async Task<PaginatedResultDto<Event>> GetEventsAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var (events, totalCount) = await eventRepo.GetEventsAsync(title, from, to, page, pageSize, ct);
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResultDto<Event>(events, totalCount, page, pageSize, totalPages);
    }

    public async Task<Event> GetEventByIdAsync(Guid id, CancellationToken ct = default)
    {
        Event eventById = await eventRepo.GetEventByIdAsync(id, ct) ??
            throw new KeyNotFoundException($"Событие с id = {id} не найдено");

        return eventById;
    }

    public async Task<Event> AddEventAsync(Event newEvent, CancellationToken ct = default)
    {
        if (newEvent.EndAt <= newEvent.StartAt)
            throw new ArgumentException("Дата окончания события должна быть позже даты начала");

        await eventRepo.AddEventAsync(newEvent, ct);
        await eventRepo.SaveChangesAsync(ct);

        return newEvent;
    }

    public async Task UpdateEventAsync(Guid id, Event updatedEvent, CancellationToken ct = default)
    {
        if (updatedEvent.EndAt <= updatedEvent.StartAt)
            throw new ArgumentException("Дата окончания события должна быть позже даты начала");

        Event existingEvent = await eventRepo.GetEventByIdAsync(id, ct) ??
            throw new KeyNotFoundException($"Событие с id = {id} не найдено");

        int bookedSeats = existingEvent.TotalSeats - existingEvent.AvailableSeats;

        existingEvent.Title = updatedEvent.Title;
        existingEvent.Description = updatedEvent.Description;
        existingEvent.StartAt = updatedEvent.StartAt;
        existingEvent.EndAt = updatedEvent.EndAt;

        existingEvent.TotalSeats = updatedEvent.TotalSeats;
        existingEvent.AvailableSeats = Math.Max(0, existingEvent.TotalSeats - bookedSeats);

        await eventRepo.SaveChangesAsync(ct);
    }

    public async Task RemoveEventAsync(Guid id, CancellationToken ct = default)
    {
        Event existingEvent = await eventRepo.GetEventByIdAsync(id, ct) ??
            throw new KeyNotFoundException($"Событие с id = {id} не найдено");

        await eventRepo.RemoveEventAsync(existingEvent, ct);
        await eventRepo.SaveChangesAsync(ct);
    }
}