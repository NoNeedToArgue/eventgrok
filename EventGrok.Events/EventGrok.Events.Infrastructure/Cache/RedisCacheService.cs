using EventGrok.Events.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace EventGrok.Events.Infrastructure.Cache;

public class RedisCacheService(
    IConnectionMultiplexer connection,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDatabase _database = connection.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            string? json = await _database.StringGetAsync(key).WaitAsync(ct);

            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Redis get failed for key {Key}. Falling back to database", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        if (value is null)
            return;

        try
        {
            string json = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, json, ttl).WaitAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Redis set failed for key {Key}. Cache write skipped", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _database.KeyDeleteAsync(key).WaitAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Redis remove failed for key {Key}. Cache invalidation skipped", key);
        }
    }
}