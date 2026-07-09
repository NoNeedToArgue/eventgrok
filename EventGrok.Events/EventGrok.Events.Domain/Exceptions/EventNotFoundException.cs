namespace EventGrok.Events.Domain.Exceptions;

public class EventNotFoundException(Guid eventId) 
    : Exception($"Событие с id = {eventId} не найдено");