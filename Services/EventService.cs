using EventGrok.Models;

namespace EventGrok.Services;

public class EventService : IEventService
{
    private readonly List<Event> _events = [];

    public List<Event> GetEvents()
    {
        return [.. _events];
    }

    public Event? GetEventById(int id)
    {
        return _events.FirstOrDefault(e => e.Id == id);
    }

    public Event AddEvent(Event newEvent)
    {
        newEvent.Id = _events.Any() ? _events.Max(e => e.Id) + 1 : 1;
        
        _events.Add(newEvent);
        return newEvent;
    }

    public bool UpdateEvent(int id, Event updatedEvent)
    {
        int index = _events.FindIndex(e => e.Id == id);
        if (index == -1)
            return false;

        _events[index] = updatedEvent;
        return true;
    }

    public bool RemoveEvent(int id)
    {
        return _events.RemoveAll(e => e.Id == id) > 0;
    }
}