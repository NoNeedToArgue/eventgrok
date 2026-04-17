namespace EventGrok.Models;

public class Event
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    public required DateTime StartAt { get; set; }

    public required DateTime EndAt { get; set; }

    public static Event Create(string title, string description, DateTime startAt, DateTime endAt)
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
        };
    }
}