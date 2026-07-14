namespace EventGrok.Bookings.Domain.Exceptions;

public class ActiveBookingsLimitException(string message) : Exception(message);