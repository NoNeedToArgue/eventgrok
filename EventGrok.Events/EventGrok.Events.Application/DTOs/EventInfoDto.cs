namespace EventGrok.Events.Application.DTOs;

public record EventInfoDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats,
    int AvailableSeats
);