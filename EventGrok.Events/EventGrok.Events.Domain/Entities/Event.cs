using EventGrok.Events.Domain.Exceptions;

namespace EventGrok.Events.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    public required DateTime StartAt { get; set; }

    public required DateTime EndAt { get; set; }

    public required int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public static Event Create(string title, string description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        if (totalSeats <= 0)
            throw new InvalidEventException("Количество мест должно быть больше нуля");

        if (endAt <= startAt)
            throw new InvalidEventException("Дата окончания события должна быть позже даты начала");

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }

    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count) return false;
        AvailableSeats -= count;
        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats = Math.Min(TotalSeats, AvailableSeats + count);
    }
}