namespace EventGrok.Models;

public record EventInfoDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartAt,
    DateTime EndAt
);