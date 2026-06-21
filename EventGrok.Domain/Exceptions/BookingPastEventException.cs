namespace EventGrok.Domain.Exceptions;

public class BookingPastEventException(string message) : Exception(message);