using Microsoft.EntityFrameworkCore;
using EventGrok.Infrastructure.Data;
using EventGrok.Infrastructure.Repositories;
using EventGrok.Domain.Entities;
using EventGrok.IntegrationTests.Fixtures;
using EventGrok.IntegrationTests.CollectionDefinitions;

namespace EventGrok.IntegrationTests;

[Collection(nameof(PostgresTestCollection))]
public class BookingRepositoryTests(PostgresContainerFixture fixture)
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

    private static Booking CreateValidBooking(Guid eventId, BookingStatus status = BookingStatus.Pending) => new()
    {
        Id = Guid.NewGuid(),
        EventId = eventId,
        Status = status,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task AddBookingAsync_ValidBooking_SavesToDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var eventRepo = new EventRepository(context);
        var bookingRepo = new BookingRepository(context);

        Event testEvent = CreateValidEvent();
        await eventRepo.AddEventAsync(testEvent);
        await eventRepo.SaveChangesAsync();

        Booking booking = CreateValidBooking(testEvent.Id);

        // Act
        await bookingRepo.AddBookingAsync(booking);
        await bookingRepo.SaveChangesAsync();

        // Assert
        Booking? savedBooking = await bookingRepo.GetBookingByIdAsync(booking.Id);
        Assert.NotNull(savedBooking);
        Assert.Equal(booking.EventId, savedBooking.EventId);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingId_ReturnsBooking()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var eventRepo = new EventRepository(context);
        var bookingRepo = new BookingRepository(context);

        Event testEvent = CreateValidEvent();
        await eventRepo.AddEventAsync(testEvent);
        await eventRepo.SaveChangesAsync();

        Booking booking = CreateValidBooking(testEvent.Id);
        await bookingRepo.AddBookingAsync(booking);
        await bookingRepo.SaveChangesAsync();

        // Act
        Booking? retrievedBooking = await bookingRepo.GetBookingByIdAsync(booking.Id);

        // Assert
        Assert.NotNull(retrievedBooking);
        Assert.Equal(booking.Id, retrievedBooking.Id);
    }

    [Fact]
    public async Task GetPendingBookingsAsync_OnlyReturnsPendingStatus()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var eventRepo = new EventRepository(context);
        var bookingRepo = new BookingRepository(context);

        Event testEvent = CreateValidEvent();
        await eventRepo.AddEventAsync(testEvent);
        await eventRepo.SaveChangesAsync();

        Booking pending1 = CreateValidBooking(testEvent.Id, BookingStatus.Pending);
        Booking pending2 = CreateValidBooking(testEvent.Id, BookingStatus.Pending);
        Booking confirmed = CreateValidBooking(testEvent.Id, BookingStatus.Confirmed);

        await bookingRepo.AddBookingAsync(pending1);
        await bookingRepo.AddBookingAsync(pending2);
        await bookingRepo.AddBookingAsync(confirmed);
        await bookingRepo.SaveChangesAsync();

        // Act
        IReadOnlyList<Booking> pendingBookings = await bookingRepo.GetPendingBookingsAsync();

        // Assert
        Assert.Equal(2, pendingBookings.Count);
        Assert.Contains(pendingBookings, b => b.Id == pending1.Id);
        Assert.Contains(pendingBookings, b => b.Id == pending2.Id);
    }

    [Fact]
    public async Task UpdateBookingAsync_StatusChange_PersistedAfterSave()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var eventRepo = new EventRepository(context);
        var bookingRepo = new BookingRepository(context);

        Event testEvent = CreateValidEvent();
        await eventRepo.AddEventAsync(testEvent);
        await eventRepo.SaveChangesAsync();

        Booking booking = CreateValidBooking(testEvent.Id);
        await bookingRepo.AddBookingAsync(booking);
        await bookingRepo.SaveChangesAsync();

        // Act
        Booking? trackedBooking = await bookingRepo.GetBookingByIdAsync(booking.Id);

        if (trackedBooking is not null)
        {
            trackedBooking.Status = BookingStatus.Confirmed;
            trackedBooking.ProcessedAt = DateTime.UtcNow;
        }

        await bookingRepo.SaveChangesAsync();

        // Assert
        Booking? updatedBooking = await bookingRepo.GetBookingByIdAsync(booking.Id);
        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Confirmed, updatedBooking.Status);
        Assert.NotNull(updatedBooking.ProcessedAt);
    }
}