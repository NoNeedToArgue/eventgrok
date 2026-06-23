namespace EventGrok.Domain.Exceptions;

public class BookingNotFoundException(Guid bookingId) 
    : Exception($"Бронирование с id = {bookingId} не найдено");