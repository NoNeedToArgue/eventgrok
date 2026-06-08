using EventGrok.Domain.Entities;
using EventGrok.Application.DTOs;

namespace EventGrok.Application.Services;

public interface IEventService
{
    Task<PaginatedResultDto<Event>> GetEventsAsync(
        string? title, 
        DateTime? from, 
        DateTime? to, 
        int page = 1, 
        int pageSize = 10,
        CancellationToken ct = default);
    
    Task<Event> GetEventByIdAsync(Guid id, CancellationToken ct = default);
    
    Task<Event> AddEventAsync(Event newEvent, CancellationToken ct = default);
    
    Task UpdateEventAsync(Guid id, Event updatedEvent, CancellationToken ct = default);
    
    Task RemoveEventAsync(Guid id, CancellationToken ct = default);
}