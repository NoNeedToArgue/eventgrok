namespace EventGrok.Events.Application.Cache;

public class CacheSettings
{
    public int EventTtlMinutes { get; init; } = 5;

    public int TopEventsTtlMinutes { get; init; } = 10;

    public TimeSpan EventTtl => TimeSpan.FromMinutes(EventTtlMinutes);

    public TimeSpan TopEventsTtl => TimeSpan.FromMinutes(TopEventsTtlMinutes);
}
