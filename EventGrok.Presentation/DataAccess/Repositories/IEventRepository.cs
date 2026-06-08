using EventGrok.Models;

namespace EventGrok.DataAccess.Repositories;

public interface IEventRepository
{
    Task<(IReadOnlyList<Event> Events, int TotalCount)> GetEventsAsync(
        string? title,
        DateTime? from, 
        DateTime? to, 
        int page, 
        int pageSize, 
        CancellationToken ct = default);

    Task<Event?> GetEventByIdAsync(Guid id, CancellationToken ct = default);

    Task AddEventAsync(Event newEvent, CancellationToken ct = default);

    Task RemoveEventAsync(Event existingEvent, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}