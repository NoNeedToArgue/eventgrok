using EventGrok.DataAccess;
using EventGrok.Models;
using EventGrok.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventGrok.Tests;

public class EventServiceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;

    public EventServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));
        
        services.AddScoped<IEventService, EventService>();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    private IEventService GetEventService() =>
        _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IEventService>();
        
    private static Event CreateValidEvent(string title = "Test Event", int totalSeats = 100) =>
        Event.Create(title, "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), totalSeats);

    [Fact]
    [Trait("Category", "AddEvent")]
    [Trait("Data", "Valid")]
    public async Task AddEvent_ValidEvent_ReturnsEventWithId()
    {
        // Arrange
        IEventService service = GetEventService();
        Event newEvent = CreateValidEvent("Концерт");

        // Act
        Event result = await service.AddEventAsync(newEvent);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Концерт", result.Title);
    }

    [Fact]
    [Trait("Category", "AddEvent")]
    [Trait("Data", "Invalid")]
    public async Task AddEvent_InvalidDates_ThrowsArgumentException()
    {
        // Arrange
        IEventService service = GetEventService();
        var newEvent = Event.Create(
            "Концерт с некорректной датой",
            "Description",
            DateTime.UtcNow.AddHours(2),
            DateTime.UtcNow.AddHours(1),
            100
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddEventAsync(newEvent));
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEvents_EmptyService_ReturnsEmptyResult()
    {
        // Act
        IEventService service = GetEventService();
        PaginatedResultDto<Event> result = await service.GetEventsAsync(null, null, null);

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
        IEventService service = GetEventService();
        await service.AddEventAsync(CreateValidEvent("Концерт"));
        await service.AddEventAsync(CreateValidEvent("Вернисаж"));

        // Act
        PaginatedResultDto<Event> result = await service.GetEventsAsync(null, null, null);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEvents_NoMatches_ReturnsEmptyResult()
    {
        // Arrange
        IEventService service = GetEventService();
        await service.AddEventAsync(CreateValidEvent("Концерт"));
        await service.AddEventAsync(CreateValidEvent("Вернисаж"));

        // Act
        PaginatedResultDto<Event> result = await service.GetEventsAsync("Аквадискотека", null, null);

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
        IEventService service = GetEventService();
        await service.AddEventAsync(CreateValidEvent("Концерт"));
        await service.AddEventAsync(CreateValidEvent("Вернисаж"));

        // Act
        PaginatedResultDto<Event> result = await service.GetEventsAsync("концерт", null, null);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Концерт", result.Items[0].Title);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEvents_FilterByDate_ReturnsMatching()
    {
        // Arrange
        IEventService service = GetEventService();

        Event pastEvent = CreateValidEvent("Концерт");
        pastEvent.StartAt = DateTime.UtcNow.AddHours(-10);
        pastEvent.EndAt = DateTime.UtcNow.AddHours(-9);

        Event futureEvent = CreateValidEvent("Кинофестиваль");
        futureEvent.StartAt = DateTime.UtcNow.AddHours(1);
        futureEvent.EndAt = DateTime.UtcNow.AddHours(2);

        await service.AddEventAsync(pastEvent);
        await service.AddEventAsync(futureEvent);

        // Act
        PaginatedResultDto<Event> result = await service.GetEventsAsync(null, DateTime.UtcNow, null);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Кинофестиваль", result.Items[0].Title);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEvents_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        IEventService service = GetEventService();
        for (int i = 1; i <= 25; i++)
            await service.AddEventAsync(CreateValidEvent($"Событие {i}"));

        // Act
        PaginatedResultDto<Event> result = await service.GetEventsAsync(null, null, null, page: 2, pageSize: 10);

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
        IEventService service = GetEventService();

        Event event1 = CreateValidEvent("Предстоящий концерт");
        event1.StartAt = DateTime.UtcNow.AddHours(1);
        event1.EndAt = DateTime.UtcNow.AddHours(2);

        Event event2 = CreateValidEvent("Прошедший концерт");
        event2.StartAt = DateTime.UtcNow.AddHours(-10);
        event2.EndAt = DateTime.UtcNow.AddHours(-9);

        Event event3 = CreateValidEvent("Предстоящий вернисаж");
        event3.StartAt = DateTime.UtcNow.AddHours(1);
        event3.EndAt = DateTime.UtcNow.AddHours(2);

        await service.AddEventAsync(event1);
        await service.AddEventAsync(event2);
        await service.AddEventAsync(event3);

        // Act
        PaginatedResultDto<Event> result = await service.GetEventsAsync("концерт", DateTime.UtcNow, null);

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
        IEventService service = GetEventService();
        Event uniqueEvent = await service.AddEventAsync(CreateValidEvent("Голая вечеринка"));

        // Act
        Event result = await service.GetEventByIdAsync(uniqueEvent.Id);

        // Assert
        Assert.Equal("Голая вечеринка", result.Title);
    }

    [Fact]
    [Trait("Category", "GetEventById")]
    [Trait("Data", "Invalid")]
    public async Task GetEventById_InvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        IEventService service = GetEventService();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEventByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    [Trait("Category", "UpdateEvent")]
    [Trait("Data", "Valid")]
    public async Task UpdateEvent_ValidId_UpdatesEvent()
    {
        // Arrange
        IEventService service = GetEventService();
        Event oldEvent = await service.AddEventAsync(CreateValidEvent("Голая вечеринка"));
        Event updatedEvent = CreateValidEvent("Одетая вечеринка");
        updatedEvent.Id = oldEvent.Id;

        // Act
        await service.UpdateEventAsync(oldEvent.Id, updatedEvent);
        Event result = await service.GetEventByIdAsync(oldEvent.Id);

        // Assert
        Assert.Equal("Одетая вечеринка", result.Title);
    }

    [Fact]
    [Trait("Category", "UpdateEvent")]
    [Trait("Data", "Invalid")]
    public async Task UpdateEvent_InvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        IEventService service = GetEventService();
        Event updatedEvent = CreateValidEvent();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateEventAsync(Guid.NewGuid(), updatedEvent));
    }

    [Fact]
    [Trait("Category", "UpdateEvent")]
    [Trait("Data", "Invalid")]
    public async Task UpdateEvent_InvalidDates_ThrowsArgumentException()
    {
        // Arrange
        IEventService service = GetEventService();
        Event oldEvent = await service.AddEventAsync(CreateValidEvent("Хороший концерт"));
        Event updatedEvent = CreateValidEvent("Плохой концерт");
        updatedEvent.Id = oldEvent.Id;
        updatedEvent.EndAt = updatedEvent.StartAt.AddHours(-1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateEventAsync(oldEvent.Id, updatedEvent));
    }

    [Fact]
    [Trait("Category", "RemoveEvent")]
    [Trait("Data", "Valid")]
    public async Task RemoveEvent_ValidId_RemovesEvent()
    {
        // Arrange
        IEventService service = GetEventService();
        Event doomedEvent = await service.AddEventAsync(CreateValidEvent());

        // Act
        await service.RemoveEventAsync(doomedEvent.Id);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEventByIdAsync(doomedEvent.Id));
    }

    [Fact]
    [Trait("Category", "Event.ReleaseSeats")]
    public async Task ReleaseSeats_IncreasesAvailableSeats()
    {
        // Arrange
        IEventService service = GetEventService();
        Event testEvent = CreateValidEvent("Концерт", totalSeats: 2);
        await service.AddEventAsync(testEvent);

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
        IEventService service = GetEventService();
        Event testEvent = CreateValidEvent("Концерт", totalSeats: 3);
        await service.AddEventAsync(testEvent);

        // Act
        testEvent.ReleaseSeats(10);

        // Assert
        Assert.Equal(3, testEvent.AvailableSeats);
    }
}
