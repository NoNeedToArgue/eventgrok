using EventGrok.Models;
using EventGrok.Services;

namespace EventGrok.Tests;

public class EventServiceTests
{
    private readonly EventService _service;

    public EventServiceTests()
    {
        _service = new EventService();
    }

    private static Event CreateValidEvent(string title = "Test Event", int totalSeats = 100) =>
        Event.Create(title, "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), totalSeats);

    [Fact]
    [Trait("Category", "AddEvent")]
    [Trait("Data", "Valid")]
    public void AddEvent_ValidEvent_ReturnsEventWithId()
    {
        // Arrange
        Event newEvent = CreateValidEvent("Концерт");

        // Act
        Event result = _service.AddEvent(newEvent);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Концерт", result.Title);
    }

    [Fact]
    [Trait("Category", "AddEvent")]
    [Trait("Data", "Invalid")]
    public void AddEvent_InvalidDates_ThrowsArgumentException()
    {
        // Arrange
        var newEvent = Event.Create(
            "Концерт с некорректной датой",
            "Description",
            DateTime.UtcNow.AddHours(2),
            DateTime.UtcNow.AddHours(1),
            100
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.AddEvent(newEvent));
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public void GetEvents_EmptyService_ReturnsEmptyResult()
    {
        // Act
        PaginatedResultDto<Event> result = _service.GetEvents(null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public void GetEvents_WithEvents_ReturnsAll()
    {
        // Arrange
        _service.AddEvent(CreateValidEvent("Концерт"));
        _service.AddEvent(CreateValidEvent("Вернисаж"));

        // Act
        PaginatedResultDto<Event> result = _service.GetEvents(null, null, null);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public void GetEvents_NoMatches_ReturnsEmptyResult()
    {
        // Arrange
        _service.AddEvent(CreateValidEvent("Концерт"));
        _service.AddEvent(CreateValidEvent("Вернисаж"));

        // Act
        PaginatedResultDto<Event> result = _service.GetEvents("Аквадискотека", null, null);

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
    public void GetEvents_FilterByTitleWithCaseInsensitive_ReturnsMatching()
    {
        // Arrange
        _service.AddEvent(CreateValidEvent("Концерт"));
        _service.AddEvent(CreateValidEvent("Вернисаж"));

        // Act
        PaginatedResultDto<Event> result = _service.GetEvents("концерт", null, null);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Концерт", result.Items[0].Title);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public void GetEvents_FilterByDate_ReturnsMatching()
    {
        // Arrange
        Event pastEvent = CreateValidEvent("Концерт");
        pastEvent.StartAt = DateTime.UtcNow.AddHours(-10);
        pastEvent.EndAt = DateTime.UtcNow.AddHours(-9);

        Event futureEvent = CreateValidEvent("Кинофестиваль");
        futureEvent.StartAt = DateTime.UtcNow.AddHours(1);
        futureEvent.EndAt = DateTime.UtcNow.AddHours(2);

        _service.AddEvent(pastEvent);
        _service.AddEvent(futureEvent);

        // Act
        PaginatedResultDto<Event> result = _service.GetEvents(null, DateTime.UtcNow, null);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Кинофестиваль", result.Items[0].Title);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public void GetEvents_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
            _service.AddEvent(CreateValidEvent($"Событие {i}"));

        // Act
        PaginatedResultDto<Event> result = _service.GetEvents(null, null, null, page: 2, pageSize: 10);

        // Assert
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public void GetEvents_CombinedFilter_Works()
    {
        // Arrange
        Event event1 = CreateValidEvent("Предстоящий концерт");
        event1.StartAt = DateTime.UtcNow.AddHours(1);
        event1.EndAt = DateTime.UtcNow.AddHours(2);

        Event event2 = CreateValidEvent("Прошедший концерт");
        event2.StartAt = DateTime.UtcNow.AddHours(-10);
        event2.EndAt = DateTime.UtcNow.AddHours(-9);

        Event event3 = CreateValidEvent("Предстоящий вернисаж");
        event3.StartAt = DateTime.UtcNow.AddHours(1);
        event3.EndAt = DateTime.UtcNow.AddHours(2);

        _service.AddEvent(event1);
        _service.AddEvent(event2);
        _service.AddEvent(event3);

        // Act
        PaginatedResultDto<Event> result = _service.GetEvents("концерт", DateTime.UtcNow, null);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Предстоящий концерт", result.Items[0].Title);
    }

    [Fact]
    [Trait("Category", "GetEventById")]
    [Trait("Data", "Valid")]
    public void GetEventById_ValidId_ReturnsEvent()
    {
        // Arrange
        Event uniqueEvent = _service.AddEvent(CreateValidEvent("Голая вечеринка"));

        // Act
        Event result = _service.GetEventById(uniqueEvent.Id);

        // Assert
        Assert.Equal("Голая вечеринка", result.Title);
    }

    [Fact]
    [Trait("Category", "GetEventById")]
    [Trait("Data", "Invalid")]
    public void GetEventById_InvalidId_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _service.GetEventById(Guid.NewGuid()));
    }

    [Fact]
    [Trait("Category", "UpdateEvent")]
    [Trait("Data", "Valid")]
    public void UpdateEvent_ValidId_UpdatesEvent()
    {
        // Arrange
        Event oldEvent = _service.AddEvent(CreateValidEvent("Голая вечеринка"));
        Event updatedEvent = CreateValidEvent("Одетая вечеринка");
        updatedEvent.Id = oldEvent.Id;

        // Act
        _service.UpdateEvent(oldEvent.Id, updatedEvent);
        Event result = _service.GetEventById(oldEvent.Id);

        // Assert
        Assert.Equal("Одетая вечеринка", result.Title);
    }

    [Fact]
    [Trait("Category", "UpdateEvent")]
    [Trait("Data", "Invalid")]
    public void UpdateEvent_InvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        Event updatedEvent = CreateValidEvent();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _service.UpdateEvent(Guid.NewGuid(), updatedEvent));
    }

    [Fact]
    [Trait("Category", "UpdateEvent")]
    [Trait("Data", "Invalid")]
    public void UpdateEvent_InvalidDates_ThrowsArgumentException()
    {
        // Arrange
        Event oldEvent = _service.AddEvent(CreateValidEvent("Хороший концерт"));
        Event updatedEvent = CreateValidEvent("Плохой концерт");
        updatedEvent.Id = oldEvent.Id;
        updatedEvent.EndAt = updatedEvent.StartAt.AddHours(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.UpdateEvent(oldEvent.Id, updatedEvent));
    }

    [Fact]
    [Trait("Category", "RemoveEvent")]
    [Trait("Data", "Valid")]
    public void RemoveEvent_ValidId_RemovesEvent()
    {
        // Arrange
        Event doomedEvent = _service.AddEvent(CreateValidEvent());

        // Act
        _service.RemoveEvent(doomedEvent.Id);

        // Assert
        Assert.Throws<KeyNotFoundException>(() => _service.GetEventById(doomedEvent.Id));
    }
}
