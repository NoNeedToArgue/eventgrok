using Microsoft.EntityFrameworkCore;
using EventGrok.Infrastructure.Data;
using EventGrok.Infrastructure.Repositories;
using EventGrok.Domain.Entities;
using EventGrok.IntegrationTests.Fixtures;
using EventGrok.IntegrationTests.CollectionDefinitions;

namespace EventGrok.IntegrationTests;

[Collection(nameof(PostgresTestCollection))]
public class EventRepositoryTests(PostgresContainerFixture fixture)
{
    private async Task<AppDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = await CreateContextAsync();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE bookings, events RESTART IDENTITY CASCADE");
    }

    private static Event CreateValidEvent(string title = "Test Event", int totalSeats = 100) =>
        Event.Create(title, "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), totalSeats);

    [Fact]
    public async Task AddEventAsync_ValidEvent_SavesToDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var repo = new EventRepository(context);

        Event newEvent = CreateValidEvent("Концерт");

        // Act
        await repo.AddEventAsync(newEvent);
        await repo.SaveChangesAsync();

        // Assert
        Event? savedEvent = await repo.GetEventByIdAsync(newEvent.Id);
        Assert.NotNull(savedEvent);
        Assert.Equal("Концерт", savedEvent.Title);
    }

    [Fact]
    public async Task GetEventsAsync_EmptyDatabase_ReturnsEmptyResult()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var repo = new EventRepository(context);

        // Act
        var (items, totalCount) = await repo.GetEventsAsync(null, null, null);

        // Assert
        Assert.Empty(items);
        Assert.Equal(0, totalCount);
    }

    [Fact]
    public async Task GetEventsAsync_WithEvents_ReturnsAll()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var repo = new EventRepository(context);

        await repo.AddEventAsync(CreateValidEvent("Концерт"));
        await repo.AddEventAsync(CreateValidEvent("Вернисаж"));
        await repo.SaveChangesAsync();

        // Act
        var (items, totalCount) = await repo.GetEventsAsync(null, null, null);

        // Assert
        Assert.Equal(2, totalCount);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetEventsAsync_FilterByTitle_ReturnsMatching()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var repo = new EventRepository(context);

        await repo.AddEventAsync(CreateValidEvent("Концерт"));
        await repo.AddEventAsync(CreateValidEvent("Вернисаж"));
        await repo.SaveChangesAsync();

        // Act
        var (items, totalCount) = await repo.GetEventsAsync("концерт", null, null);

        // Assert
        Assert.Single(items);
        Assert.Equal(1, totalCount);
        Assert.Equal("Концерт", items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_FilterByDate_ReturnsMatching()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var repo = new EventRepository(context);

        Event pastEvent = CreateValidEvent("Прошедший концерт");
        pastEvent.StartAt = DateTime.UtcNow.AddHours(-10);
        pastEvent.EndAt = DateTime.UtcNow.AddHours(-9);

        Event futureEvent = CreateValidEvent("Предстоящий концерт");
        futureEvent.StartAt = DateTime.UtcNow.AddHours(1);
        futureEvent.EndAt = DateTime.UtcNow.AddHours(2);

        await repo.AddEventAsync(pastEvent);
        await repo.AddEventAsync(futureEvent);
        await repo.SaveChangesAsync();

        // Act
        var (items, totalCount) = await repo.GetEventsAsync(null, DateTime.UtcNow, null);

        // Assert
        Assert.Single(items);
        Assert.Equal("Предстоящий концерт", items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_CombinedFilters_ReturnsMatching()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var repo = new EventRepository(context);

        Event event1 = CreateValidEvent("Предстоящий концерт");
        event1.StartAt = DateTime.UtcNow.AddHours(1);
        event1.EndAt = DateTime.UtcNow.AddHours(2);
        Event event2 = CreateValidEvent("Прошедший концерт");
        event2.StartAt = DateTime.UtcNow.AddHours(-10);
        event2.EndAt = DateTime.UtcNow.AddHours(-9);
        Event event3 = CreateValidEvent("Предстоящая выставка");
        event3.StartAt = DateTime.UtcNow.AddHours(1);
        event3.EndAt = DateTime.UtcNow.AddHours(2);

        await repo.AddEventAsync(event1);
        await repo.AddEventAsync(event2);
        await repo.AddEventAsync(event3);
        await repo.SaveChangesAsync();

        // Act
        var (items, totalCount) = await repo.GetEventsAsync("концерт", DateTime.UtcNow, null);

        Assert.Single(items);
        Assert.Equal("Предстоящий концерт", items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_Pagination_ReturnsCorrectCounts()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var repo = new EventRepository(context);

        for (var i = 1; i <= 15; i++)
            await repo.AddEventAsync(CreateValidEvent($"Событие {i}"));
        await repo.SaveChangesAsync();

        // Act
        var (page1, total1) = await repo.GetEventsAsync(null, null, null, 1, 10);
        var (page2, total2) = await repo.GetEventsAsync(null, null, null, 2, 10);

        // Assert
        Assert.Equal(15, total1);
        Assert.Equal(10, page1.Count);
        Assert.Equal(5, page2.Count);
    }

    [Fact]
    public async Task UpdateEventAsync_ValidEvent_UpdatesFields()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var repo = new EventRepository(context);

        Event originalEvent = CreateValidEvent("Старое название");
        await repo.AddEventAsync(originalEvent);
        await repo.SaveChangesAsync();

        // Act
        Event? fetchedEvent = await repo.GetEventByIdAsync(originalEvent.Id);
        if (fetchedEvent is not null)
        {
            fetchedEvent.Title = "Новое название";
            fetchedEvent.TotalSeats = 500;
        }

        await repo.SaveChangesAsync();

        // Assert
        Event? updatedEvent = await repo.GetEventByIdAsync(originalEvent.Id);
        Assert.NotNull(updatedEvent);
        Assert.Equal("Новое название", updatedEvent.Title);
        Assert.Equal(500, updatedEvent.TotalSeats);
    }

    [Fact]
    public async Task RemoveEventAsync_ValidEvent_RemovesFromDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var repo = new EventRepository(context);

        Event doomedEvent = CreateValidEvent("Голая вечеринка");
        await repo.AddEventAsync(doomedEvent);
        await repo.SaveChangesAsync();

        // Act
        await repo.RemoveEventAsync(doomedEvent);
        await repo.SaveChangesAsync();

        // Assert
        Event? deletedEvent = await repo.GetEventByIdAsync(doomedEvent.Id);
        Assert.Null(deletedEvent);
    }
}