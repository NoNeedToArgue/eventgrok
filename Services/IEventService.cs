using EventGrok.Models;

namespace EventGrok.Services;

public interface IEventService
{
    PaginatedResultDto<Event> GetEvents(
        string? title, 
        DateTime? from, 
        DateTime? to, 
        int page = 1, 
        int pageSize = 10);
    
    Event GetEventById(Guid id);
    
    Event AddEvent(Event newEvent);
    
    void UpdateEvent(Guid id, Event updatedEvent);
    
    void RemoveEvent(Guid id);
}