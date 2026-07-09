namespace EventGrok.Events.Application.DTOs;

public record PaginatedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);