namespace EventGrok.Contracts.Events;

public sealed record BookingConfirmed
{
    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid UserId { get; init; }
    public int SeatsCount { get; init; } = 1;
    public required DateTime ConfirmedAt { get; init; }
}
