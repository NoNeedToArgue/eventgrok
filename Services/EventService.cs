using EventGrok.Models;

namespace EventGrok.Services;

public class EventService : IEventService
{
    private readonly List<Event> _events = [];

    private int FindEventIndex(int id)
    {
        int index = _events.FindIndex(e => e.Id == id);
        if (index == -1)
            throw new KeyNotFoundException($"Событие с id = {id} не найдено");

        return index;
    }

    public List<Event> GetEvents(string? title, DateTime? from, DateTime? to)
    {
        List<Event> events = [.. _events
            .Where(e => title is null || e.Title.Contains(title, StringComparison.OrdinalIgnoreCase))
            .Where(e => from is null || e.StartAt >= from)
            .Where(e => to is null || e.EndAt <= to)];

        return events;
    }

    public Event GetEventById(int id)
    {
        Event eventById = _events.FirstOrDefault(e => e.Id == id) ??
            throw new KeyNotFoundException($"Событие с id = {id} не найдено");

        return eventById;
    }

    public Event AddEvent(Event newEvent)
    {
        if (newEvent.EndAt <= newEvent.StartAt)
            throw new ArgumentException("Дата окончания события должна быть позже даты начала");

        newEvent.Id = _events.Any() ? _events.Max(e => e.Id) + 1 : 1;

        _events.Add(newEvent);

        return newEvent;
    }

    public void UpdateEvent(int id, Event updatedEvent)
    {
        int index = FindEventIndex(id);

        if (updatedEvent.EndAt <= updatedEvent.StartAt)
            throw new ArgumentException("Дата окончания события должна быть позже даты начала");

        _events[index] = updatedEvent;
    }

    public void RemoveEvent(int id)
    {
        int index = FindEventIndex(id);

        _events.RemoveAt(index);
    }
}