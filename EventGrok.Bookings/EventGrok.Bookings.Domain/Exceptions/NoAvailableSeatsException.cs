namespace EventGrok.Bookings.Domain.Exceptions;

public class NoAvailableSeatsException(string message) : Exception(message);