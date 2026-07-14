namespace EventGrok.Bookings.Domain.Exceptions;

public class NotOwnBookingException() : Exception("Можно отменять только свои бронирования");