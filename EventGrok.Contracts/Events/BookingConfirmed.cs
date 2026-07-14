namespace EventGrok.Contracts.Events;

public sealed record BookingConfirmed
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid UserId { get; init; }
    public required int SeatsCount { get; init; }
    public required DateTime ConfirmedAt { get; init; }
}
