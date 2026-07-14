namespace EventGrok.Events.Domain.Exceptions;

public class InvalidEventException(string message) : Exception(message);