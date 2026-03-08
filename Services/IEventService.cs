using EventGrok.Models;

namespace EventGrok.Services;

public interface IEventService
{
    List<Event> GetEvents();
    
    Event? GetEventById(int id);
    
    Event AddEvent(Event newEvent);
    
    bool UpdateEvent(int id, Event updatedEvent);
    
    bool RemoveEvent(int id);
}