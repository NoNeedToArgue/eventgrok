using EventGrok.Events.Infrastructure.Data;
using EventGrok.Events.Domain.Entities;
using EventGrok.Events.Domain.Exceptions;
using EventGrok.Events.Application.Services;
using EventGrok.Events.Application.DTOs;
using EventGrok.Events.Application.Interfaces;
using EventGrok.Events.Infrastructure.Repositories;
using EventGrok.Events.Application.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventGrok.Events.Tests;

public class EventServiceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;

    public EventServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<EventsDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        services.AddScoped<IEventRepository, EventRepository>();

        services.AddSingleton<ICacheService>(new NullCacheService());

        services.AddSingleton<IOptionsMonitor<CacheSettings>>(new TestCacheSettingsMonitor());

        services.AddScoped<IEventService, EventService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    private static Event CreateValidEvent(string title = "Test Event", int totalSeats = 100) =>
        Event.Create(title, "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), totalSeats);

    private static CreateEventDto CreateValidEventDto(string title = "Test Event", int totalSeats = 100) =>
        new() { Title = title, StartAt = DateTime.UtcNow.AddHours(1), EndAt = DateTime.UtcNow.AddHours(2), TotalSeats = totalSeats };

    [Fact]
    [Trait("Category", "AddEvent")]
    [Trait("Data", "Valid")]
    public async Task AddEvent_ValidEvent_ReturnsEventWithId()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        CreateEventDto newEvent = CreateValidEventDto("Концерт");

        // Act
        EventInfoDto result = await eventService.CreateEventAsync(newEvent);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Концерт", result.Title);
    }

    [Fact]
    [Trait("Category", "AddEvent")]
    [Trait("Data", "Invalid")]
    public async Task AddEvent_InvalidDates_ThrowsInvalidEventException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        CreateEventDto newEvent = CreateValidEventDto();

        newEvent.Title = "Концерт с некорректной датой";
        newEvent.StartAt = DateTime.UtcNow.AddHours(2);
        newEvent.EndAt = DateTime.UtcNow.AddHours(1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidEventException>(() => eventService.CreateEventAsync(newEvent));
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEvents_EmptyService_ReturnsEmptyResult()
    {
        // Act
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        PaginatedResultDto<EventInfoDto> result = await eventService.GetEventsAsync(null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEvents_WithEvents_ReturnsAll()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        await eventService.CreateEventAsync(CreateValidEventDto("Концерт"));
        await eventService.CreateEventAsync(CreateValidEventDto("Вернисаж"));

        // Act
        PaginatedResultDto<EventInfoDto> result = await eventService.GetEventsAsync(null, null, null);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEvents_NoMatches_ReturnsEmptyResult()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        await eventService.CreateEventAsync(CreateValidEventDto("Концерт"));
        await eventService.CreateEventAsync(CreateValidEventDto("Вернисаж"));

        // Act
        PaginatedResultDto<EventInfoDto> result = await eventService.GetEventsAsync("Аквадискотека", null, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEvents_FilterByTitleWithCaseInsensitive_ReturnsMatching()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        await eventService.CreateEventAsync(CreateValidEventDto("Концерт"));
        await eventService.CreateEventAsync(CreateValidEventDto("Вернисаж"));

        // Act
        PaginatedResultDto<EventInfoDto> result = await eventService.GetEventsAsync("концерт", null, null);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Концерт", result.Items[0].Title);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEvents_FilterByDate_ReturnsMatching()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        CreateEventDto pastEvent = CreateValidEventDto("Концерт");
        pastEvent.StartAt = DateTime.UtcNow.AddHours(-10);
        pastEvent.EndAt = DateTime.UtcNow.AddHours(-9);

        CreateEventDto futureEvent = CreateValidEventDto("Кинофестиваль");
        futureEvent.StartAt = DateTime.UtcNow.AddHours(1);
        futureEvent.EndAt = DateTime.UtcNow.AddHours(2);

        await eventService.CreateEventAsync(pastEvent);
        await eventService.CreateEventAsync(futureEvent);

        // Act
        PaginatedResultDto<EventInfoDto> result = await eventService.GetEventsAsync(null, DateTime.UtcNow, null);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Кинофестиваль", result.Items[0].Title);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEvents_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        for (int i = 1; i <= 25; i++)
            await eventService.CreateEventAsync(CreateValidEventDto($"Событие {i}"));

        // Act
        PaginatedResultDto<EventInfoDto> result = await eventService.GetEventsAsync(null, null, null, page: 2, pageSize: 10);

        // Assert
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEvents_CombinedFilter_Works()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        CreateEventDto event1 = CreateValidEventDto("Предстоящий концерт");
        event1.StartAt = DateTime.UtcNow.AddHours(1);
        event1.EndAt = DateTime.UtcNow.AddHours(2);

        CreateEventDto event2 = CreateValidEventDto("Прошедший концерт");
        event2.StartAt = DateTime.UtcNow.AddHours(-10);
        event2.EndAt = DateTime.UtcNow.AddHours(-9);

        CreateEventDto event3 = CreateValidEventDto("Предстоящий вернисаж");
        event3.StartAt = DateTime.UtcNow.AddHours(1);
        event3.EndAt = DateTime.UtcNow.AddHours(2);

        await eventService.CreateEventAsync(event1);
        await eventService.CreateEventAsync(event2);
        await eventService.CreateEventAsync(event3);

        // Act
        PaginatedResultDto<EventInfoDto> result = await eventService.GetEventsAsync("концерт", DateTime.UtcNow, null);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Предстоящий концерт", result.Items[0].Title);
    }

    [Fact]
    [Trait("Category", "GetEventById")]
    [Trait("Data", "Valid")]
    public async Task GetEventById_ValidId_ReturnsEvent()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        EventInfoDto uniqueEvent = await eventService.CreateEventAsync(CreateValidEventDto("Голая вечеринка"));

        // Act
        EventInfoDto result = await eventService.GetEventByIdAsync(uniqueEvent.Id);

        // Assert
        Assert.Equal("Голая вечеринка", result.Title);
    }

    [Fact]
    [Trait("Category", "GetEventById")]
    [Trait("Data", "Invalid")]
    public async Task GetEventById_InvalidId_ThrowsEventNotFoundException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        // Act & Assert
        await Assert.ThrowsAsync<EventNotFoundException>(() => eventService.GetEventByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    [Trait("Category", "UpdateEvent")]
    [Trait("Data", "Valid")]
    public async Task UpdateEvent_ValidId_UpdatesEvent()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        EventInfoDto oldEvent = await eventService.CreateEventAsync(CreateValidEventDto("Голая вечеринка"));
        CreateEventDto updatedEvent = CreateValidEventDto("Одетая вечеринка");

        // Act
        await eventService.UpdateEventAsync(oldEvent.Id, updatedEvent);
        EventInfoDto result = await eventService.GetEventByIdAsync(oldEvent.Id);

        // Assert
        Assert.Equal("Одетая вечеринка", result.Title);
    }

    [Fact]
    [Trait("Category", "UpdateEvent")]
    [Trait("Data", "Invalid")]
    public async Task UpdateEvent_InvalidId_ThrowsEventNotFoundException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        CreateEventDto updatedEvent = CreateValidEventDto();

        // Act & Assert
        await Assert.ThrowsAsync<EventNotFoundException>(() => eventService.UpdateEventAsync(Guid.NewGuid(), updatedEvent));
    }

    [Fact]
    [Trait("Category", "UpdateEvent")]
    [Trait("Data", "Invalid")]
    public async Task UpdateEvent_InvalidDates_ThrowsInvalidEventException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        EventInfoDto oldEvent = await eventService.CreateEventAsync(CreateValidEventDto("Хороший концерт"));
        CreateEventDto updatedEvent = CreateValidEventDto("Плохой концерт");
        updatedEvent.EndAt = updatedEvent.StartAt.AddHours(-1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidEventException>(() => eventService.UpdateEventAsync(oldEvent.Id, updatedEvent));
    }

    [Fact]
    [Trait("Category", "RemoveEvent")]
    [Trait("Data", "Valid")]
    public async Task RemoveEvent_ValidId_RemovesEvent()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        EventInfoDto doomedEvent = await eventService.CreateEventAsync(CreateValidEventDto());

        // Act
        await eventService.RemoveEventAsync(doomedEvent.Id);

        // Assert
        await Assert.ThrowsAsync<EventNotFoundException>(() => eventService.GetEventByIdAsync(doomedEvent.Id));
    }

    [Fact]
    [Trait("Category", "Event.ReleaseSeats")]
    public async Task ReleaseSeats_IncreasesAvailableSeats()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        Event testEvent = CreateValidEvent("Концерт", totalSeats: 2);

        testEvent.AvailableSeats = 1;

        // Act
        testEvent.ReleaseSeats(1);

        // Assert
        Assert.Equal(2, testEvent.AvailableSeats);
    }

    [Fact]
    [Trait("Category", "Event.ReleaseSeats")]
    public async Task ReleaseSeats_DoesNotExceedTotalSeats()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        Event testEvent = CreateValidEvent("Концерт", totalSeats: 3);

        // Act
        testEvent.ReleaseSeats(10);

        // Assert
        Assert.Equal(3, testEvent.AvailableSeats);
    }

    private sealed class NullCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
            => Task.FromResult<T?>(default);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class TestCacheSettingsMonitor : IOptionsMonitor<CacheSettings>
    {
        public CacheSettings CurrentValue { get; } = new CacheSettings();

        public CacheSettings Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<CacheSettings, string?> listener) => null;
    }
}
