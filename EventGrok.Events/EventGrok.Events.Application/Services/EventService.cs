using EventGrok.Events.Domain.Entities;
using EventGrok.Events.Domain.Exceptions;
using EventGrok.Events.Application.Interfaces;
using EventGrok.Events.Application.DTOs;
using EventGrok.Events.Application.Cache;
using Microsoft.Extensions.Options;

namespace EventGrok.Events.Application.Services;

public class EventService(
    IEventRepository eventRepo,
    ICacheService cache,
    IOptionsMonitor<CacheSettings> cacheSettings) : IEventService
{
    private const int TopEventsCount = 10;

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
        string cacheKey = CacheKeys.EventById(id);

        var cached = await cache.GetAsync<EventInfoDto>(cacheKey, ct);
        if (cached is not null)
            return cached;

        Event eventById = await eventRepo.GetEventByIdAsync(id, ct) ??
            throw new EventNotFoundException(id);

        EventInfoDto dto = MapToDto(eventById);

        await cache.SetAsync(cacheKey, dto, cacheSettings.CurrentValue.EventTtl, ct);

        return dto;
    }

    public async Task<IReadOnlyList<EventInfoDto>> GetTopEventsAsync(CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<List<EventInfoDto>>(CacheKeys.TopEvents, ct);
        if (cached is not null)
            return cached;

        IReadOnlyList<Event> topEvents = await eventRepo.GetTopEventsAsync(TopEventsCount, ct);

        List<EventInfoDto> dtos = [.. topEvents.Select(MapToDto)];

        await cache.SetAsync(CacheKeys.TopEvents, dtos, cacheSettings.CurrentValue.TopEventsTtl, ct);

        return dtos;
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