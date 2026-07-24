using EventGrok.Events.Application.Services;
using EventGrok.Events.Application.Interfaces;
using EventGrok.Events.Domain.Entities;
using EventGrok.Events.Application.DTOs;
using EventGrok.Events.Application.Cache;
using Microsoft.Extensions.Options;
using Moq;

namespace EventGrok.Events.Tests;

public class EventServiceCacheTests
{
    private static (EventService service, Mock<IEventRepository> repoMock, Mock<ICacheService> cacheMock) CreateServiceWithMocks()
    {
        Mock<IEventRepository> repoMock = new();
        Mock<ICacheService> cacheMock = new();

        Mock<IOptionsMonitor<CacheSettings>> settingsMock = new();
        settingsMock.Setup(s => s.CurrentValue).Returns(new CacheSettings());

        var service = new EventService(repoMock.Object, cacheMock.Object, settingsMock.Object);

        return (service, repoMock, cacheMock);
    }

    private static Event CreateValidEvent(string title = "Test Event", int totalSeats = 100) =>
        Event.Create(title, "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), totalSeats);

    private static CreateEventDto CreateValidEventDto(string title = "Test Event", int totalSeats = 100) =>
        new()
        {
            Title = title,
            StartAt = DateTime.UtcNow.AddHours(1),
            EndAt = DateTime.UtcNow.AddHours(2),
            TotalSeats = totalSeats
        };

    [Fact]
    [Trait("Category", "Cache")]
    public async Task GetEventById_CacheHit_ReturnsFromCache_RepositoryNotCalled()
    {
        // Arrange
        var (service, repoMock, cacheMock) = CreateServiceWithMocks();

        var eventId = Guid.NewGuid();
        var cachedDto = new EventInfoDto(
            eventId,
            "From Cache",
            "Description",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(2),
            100,
            100);

        cacheMock
            .Setup(c => c.GetAsync<EventInfoDto>(CacheKeys.EventById(eventId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDto);

        // Act
        EventInfoDto result = await service.GetEventByIdAsync(eventId);

        // Assert
        Assert.Equal("From Cache", result.Title);
        repoMock.Verify(r => r.GetEventByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Cache")]
    public async Task GetEventById_CacheMiss_CallsRepository_SavesToCache()
    {
        // Arrange
        var (service, repoMock, cacheMock) = CreateServiceWithMocks();

        Event newEvent = CreateValidEvent("New Event");
        Guid eventId = newEvent.Id;

        cacheMock
            .Setup(c => c.GetAsync<EventInfoDto>(CacheKeys.EventById(eventId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventInfoDto?)null);

        repoMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newEvent);

        // Act
        EventInfoDto result = await service.GetEventByIdAsync(eventId);

        // Assert
        Assert.Equal("New Event", result.Title);
        Assert.Equal(eventId, result.Id);
        cacheMock.Verify(c => c.SetAsync(
            CacheKeys.EventById(eventId),
            It.IsAny<EventInfoDto>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Cache")]
    public async Task UpdateEvent_InvalidatesCache()
    {
        // Arrange
        var (service, repoMock, cacheMock) = CreateServiceWithMocks();

        Event existingEvent = CreateValidEvent();
        Guid eventId = existingEvent.Id;

        repoMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        CreateEventDto updateDto = CreateValidEventDto();

        // Act
        await service.UpdateEventAsync(eventId, updateDto);

        // Assert
        cacheMock.Verify(c => c.RemoveAsync(CacheKeys.EventById(eventId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Cache")]
    public async Task RemoveEvent_InvalidatesCache()
    {
        // Arrange
        var (service, repoMock, cacheMock) = CreateServiceWithMocks();

        Event existingEvent = CreateValidEvent();
        Guid eventId = existingEvent.Id;

        repoMock
            .Setup(r => r.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act
        await service.RemoveEventAsync(eventId);

        // Assert
        cacheMock.Verify(c => c.RemoveAsync(CacheKeys.EventById(eventId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Cache")]
    public async Task GetTopEvents_CacheHit_ReturnsFromCache_RepositoryNotCalled()
    {
        // Arrange
        var (service, repoMock, cacheMock) = CreateServiceWithMocks();

        List<EventInfoDto> cachedList =
        [
            new(Guid.NewGuid(), "Top 1", "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(1), 100, 10),
            new(Guid.NewGuid(), "Top 2", "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(1), 100, 20)
        ];

        cacheMock
            .Setup(c => c.GetAsync<List<EventInfoDto>>(CacheKeys.TopEvents, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedList);

        // Act
        IReadOnlyList<EventInfoDto> result = await service.GetTopEventsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Top 1", result[0].Title);
        repoMock.Verify(r => r.GetTopEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Cache")]
    public async Task GetTopEvents_CacheMiss_CallsRepository_SavesToCache()
    {
        // Arrange
        var (service, repoMock, cacheMock) = CreateServiceWithMocks();

        Event topEvent = CreateValidEvent("Top 1");

        cacheMock
            .Setup(c => c.GetAsync<List<EventInfoDto>>(CacheKeys.TopEvents, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<EventInfoDto>?)null);

        repoMock
            .Setup(r => r.GetTopEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([topEvent]);

        // Act
        IReadOnlyList<EventInfoDto> result = await service.GetTopEventsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Top 1", result[0].Title);
        cacheMock.Verify(c => c.SetAsync(
            CacheKeys.TopEvents,
            It.IsAny<List<EventInfoDto>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}