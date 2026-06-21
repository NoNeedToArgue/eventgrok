namespace EventGrok.Domain.Exceptions;

public class ActiveBookingsLimitException(string message) : Exception(message);