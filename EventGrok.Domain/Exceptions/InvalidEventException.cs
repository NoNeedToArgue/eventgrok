namespace EventGrok.Domain.Exceptions;

public class InvalidEventException(string message) : Exception(message);