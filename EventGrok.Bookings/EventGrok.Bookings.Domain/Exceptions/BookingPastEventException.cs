namespace EventGrok.Bookings.Domain.Exceptions;

public class BookingPastEventException(string message) : Exception(message);