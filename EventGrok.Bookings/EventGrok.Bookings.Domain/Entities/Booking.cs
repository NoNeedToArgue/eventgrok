namespace EventGrok.Bookings.Domain.Entities;

public class Booking
{
    public required Guid Id { get; set; }

    public required Guid EventId { get; set; }

    public required BookingStatus Status { get; set; }

    public required DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public Guid UserId { get; set; }

    public static Booking Create(Guid eventId, Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}