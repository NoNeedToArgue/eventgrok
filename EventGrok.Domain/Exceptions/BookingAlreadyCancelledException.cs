namespace EventGrok.Domain.Exceptions;

public class BookingAlreadyCancelledException() 
    : Exception("Бронирование уже отменено");