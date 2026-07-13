using Microsoft.EntityFrameworkCore;
using EventGrok.Bookings.Infrastructure.Data;
using EventGrok.Bookings.Infrastructure.Repositories;
using EventGrok.Bookings.Domain.Entities;
using EventGrok.Bookings.IntegrationTests.Fixtures;
using EventGrok.Bookings.IntegrationTests.CollectionDefinitions;

namespace EventGrok.Bookings.IntegrationTests;

[Collection(nameof(PostgresTestCollection))]
public class BookingRepositoryTests(PostgresContainerFixture fixture)
{
    private static readonly Guid TestUserId = Guid.NewGuid();

    private async Task<BookingsDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        var context = new BookingsDbContext(options);
        await context.Database.MigrateAsync();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = await CreateContextAsync();
        
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE bookings RESTART IDENTITY CASCADE");
    }

    [Fact]
    public async Task AddBookingAsync_ValidBooking_SavesToDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var bookingRepo = new BookingRepository(context);

        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Booking booking = Booking.Create(eventId, userId);

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
        var bookingRepo = new BookingRepository(context);

        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Booking booking = Booking.Create(eventId, userId);

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
        var bookingRepo = new BookingRepository(context);

        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Booking pending1 = Booking.Create(eventId, userId);
        Booking pending2 = Booking.Create(eventId, userId);
        Booking confirmed = Booking.Create(eventId, userId);
        confirmed.Confirm();

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
        var bookingRepo = new BookingRepository(context);

        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Booking booking = Booking.Create(eventId, userId);

        await bookingRepo.AddBookingAsync(booking);
        await bookingRepo.SaveChangesAsync();

        // Act
        Booking? trackedBooking = await bookingRepo.GetBookingByIdAsync(booking.Id);

        trackedBooking?.Confirm();

        await bookingRepo.SaveChangesAsync();

        // Assert
        Booking? updatedBooking = await bookingRepo.GetBookingByIdAsync(booking.Id);
        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Confirmed, updatedBooking.Status);
        Assert.NotNull(updatedBooking.ProcessedAt);
    }
}