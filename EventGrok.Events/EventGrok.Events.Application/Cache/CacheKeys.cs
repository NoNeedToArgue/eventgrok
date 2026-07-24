namespace EventGrok.Events.Application.Cache;

public static class CacheKeys
{
    public static string EventById(Guid eventId) => $"event:{eventId}";

    public const string TopEvents = "events:top10";
}
