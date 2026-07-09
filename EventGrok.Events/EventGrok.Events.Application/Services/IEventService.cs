using EventGrok.Events.Application.DTOs;

namespace EventGrok.Events.Application.Services;

public interface IEventService
{
    Task<PaginatedResultDto<EventInfoDto>> GetEventsAsync(
        string? title, 
        DateTime? from, 
        DateTime? to, 
        int page = 1, 
        int pageSize = 10,
        CancellationToken ct = default);
    
    Task<EventInfoDto> GetEventByIdAsync(Guid id, CancellationToken ct = default);
    
    Task<EventInfoDto> CreateEventAsync(CreateEventDto dto, CancellationToken ct = default);
    
    Task UpdateEventAsync(Guid id, CreateEventDto dto, CancellationToken ct = default);
    
    Task RemoveEventAsync(Guid id, CancellationToken ct = default);
}