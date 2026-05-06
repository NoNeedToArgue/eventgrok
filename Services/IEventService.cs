using EventGrok.Models;

namespace EventGrok.Services;

public interface IEventService
{
    Task<PaginatedResultDto<Event>> GetEventsAsync(
        string? title, 
        DateTime? from, 
        DateTime? to, 
        int page = 1, 
        int pageSize = 10);
    
    Task<Event> GetEventByIdAsync(Guid id);
    
    Task<Event> AddEventAsync(Event newEvent);
    
    Task UpdateEventAsync(Guid id, Event updatedEvent);
    
    Task RemoveEventAsync(Guid id);
}