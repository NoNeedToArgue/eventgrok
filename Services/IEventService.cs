using EventGrok.Models;

namespace EventGrok.Services;

public interface IEventService
{
    List<Event> GetEvents(string? title, DateTime? from, DateTime? to);
    
    Event GetEventById(int id);
    
    Event AddEvent(Event newEvent);
    
    void UpdateEvent(int id, Event updatedEvent);
    
    void RemoveEvent(int id);
}