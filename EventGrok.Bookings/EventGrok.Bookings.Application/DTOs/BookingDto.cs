namespace EventGrok.Bookings.Application.DTOs;

public record BookingDto(
    Guid Id,
    Guid EventId,
    string Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);