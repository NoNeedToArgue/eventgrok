using EventGrok.Domain.Entities;
using EventGrok.Domain.Exceptions;
using EventGrok.Application.Interfaces;
using EventGrok.Application.DTOs;

namespace EventGrok.Application.Services;

public class EventService(IEventRepository eventRepo) : IEventService
{
    public async Task<PaginatedResultDto<EventInfoDto>> GetEventsAsync(
        string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var (events, totalCount) = await eventRepo.GetEventsAsync(title, from, to, page, pageSize, ct);
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var mappedItems = events.Select(MapToDto).ToList();

        return new PaginatedResultDto<EventInfoDto>(mappedItems, totalCount, page, pageSize, totalPages);
    }

    public async Task<EventInfoDto> GetEventByIdAsync(Guid id, CancellationToken ct = default)
    {
        Event eventById = await eventRepo.GetEventByIdAsync(id, ct) ??
            throw new EventNotFoundException(id);

        return MapToDto(eventById);
    }

    public async Task<EventInfoDto> CreateEventAsync(CreateEventDto dto, CancellationToken ct = default)
    {
        Event newEvent = Event.Create(dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats);

        await eventRepo.AddEventAsync(newEvent, ct);
        await eventRepo.SaveChangesAsync(ct);

        return MapToDto(newEvent);
    }

    public async Task UpdateEventAsync(Guid id, CreateEventDto dto, CancellationToken ct = default)
    {
        if (dto.EndAt <= dto.StartAt)
            throw new InvalidEventException("Дата окончания события должна быть позже даты начала");

        Event existingEvent = await eventRepo.GetEventByIdAsync(id, ct) ??
            throw new EventNotFoundException(id);

        int bookedSeats = existingEvent.TotalSeats - existingEvent.AvailableSeats;

        existingEvent.Title = dto.Title;
        existingEvent.Description = dto.Description;
        existingEvent.StartAt = dto.StartAt;
        existingEvent.EndAt = dto.EndAt;

        existingEvent.TotalSeats = dto.TotalSeats;
        existingEvent.AvailableSeats = Math.Max(0, existingEvent.TotalSeats - bookedSeats);

        await eventRepo.SaveChangesAsync(ct);
    }

    public async Task RemoveEventAsync(Guid id, CancellationToken ct = default)
    {
        Event existingEvent = await eventRepo.GetEventByIdAsync(id, ct) ??
            throw new EventNotFoundException(id);

        await eventRepo.RemoveEventAsync(existingEvent, ct);
        await eventRepo.SaveChangesAsync(ct);
    }

    private static EventInfoDto MapToDto(Event e) => new(
        e.Id, e.Title, e.Description, e.StartAt, e.EndAt, e.TotalSeats, e.AvailableSeats);
}