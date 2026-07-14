using EventGrok.Bookings.Application.Services;
using EventGrok.Bookings.Application.Interfaces;
using EventGrok.Bookings.Application.DTOs;
using EventGrok.Bookings.Domain.Entities;
using EventGrok.Bookings.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using EventGrok.Bookings.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EventGrok.Bookings.Infrastructure.Repositories;

namespace EventGrok.Bookings.Tests;

public class BookingServiceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;
    private static readonly Guid TestUserId = Guid.NewGuid();

    public BookingServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<BookingsDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        services.AddScoped<IBookingRepository, BookingRepository>();

        services.AddScoped<IBookingService, BookingService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    [Trait("Data", "Valid")]
    public async Task CreateBookingAsync_ReturnsBookingWithPendingStatus()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventId = Guid.NewGuid();

        // Act
        BookingDto booking = await bookingService.CreateBookingAsync(eventId, TestUserId);

        // Assert
        Assert.NotNull(booking);
        Assert.Equal(eventId, booking.EventId);
        Assert.Equal("Pending", booking.Status);
        Assert.NotEqual(DateTime.MinValue, booking.CreatedAt);
        Assert.Null(booking.ProcessedAt);
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    [Trait("Data", "Valid")]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_UniqueIds()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventId = Guid.NewGuid();

        // Act
        BookingDto booking1 = await bookingService.CreateBookingAsync(eventId, TestUserId);
        BookingDto booking2 = await bookingService.CreateBookingAsync(eventId, TestUserId);
        BookingDto booking3 = await bookingService.CreateBookingAsync(eventId, TestUserId);

        // Assert
        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.NotEqual(booking2.Id, booking3.Id);
        Assert.NotEqual(booking1.Id, booking3.Id);
        Assert.Equal(eventId, booking1.EventId);
        Assert.Equal(eventId, booking2.EventId);
        Assert.Equal(eventId, booking3.EventId);
    }

    [Fact]
    [Trait("Category", "GetBookingByIdAsync")]
    [Trait("Data", "Valid")]
    public async Task GetBookingByIdAsync_ExistingId_ReturnsCorrectBooking()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventId = Guid.NewGuid();

        BookingDto createdBooking = await bookingService.CreateBookingAsync(eventId, TestUserId);

        // Act
        BookingDto retrievedBooking = await bookingService.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        Assert.NotNull(retrievedBooking);
        Assert.Equal(createdBooking.Id, retrievedBooking.Id);
        Assert.Equal(createdBooking.EventId, retrievedBooking.EventId);
        Assert.Equal(createdBooking.Status, retrievedBooking.Status);
    }

    [Fact]
    [Trait("Category", "GetBookingByIdAsync")]
    [Trait("Data", "Valid")]
    public async Task GetBookingByIdAsync_ReflectsStatusChangeAfterUpdate()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        BookingsDbContext context = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var eventId = Guid.NewGuid();

        BookingDto createdBooking = await bookingService.CreateBookingAsync(eventId, TestUserId);

        // Act
        Booking? bookingEntity = await context.Bookings.FindAsync(createdBooking.Id);
        bookingEntity!.Status = BookingStatus.Confirmed;
        bookingEntity.ProcessedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        BookingDto updatedBooking = await bookingService.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        Assert.Equal("Confirmed", updatedBooking.Status);
        Assert.NotNull(updatedBooking.ProcessedAt);
    }

    [Fact]
    [Trait("Category", "GetBookingByIdAsync")]
    [Trait("Data", "Invalid")]
    public async Task GetBookingByIdAsync_NonExistingId_ThrowsBookingNotFoundException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        Guid nonExistingBookingId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<BookingNotFoundException>(
            () => bookingService.GetBookingByIdAsync(nonExistingBookingId));
    }

    [Fact]
    [Trait("Category", "Booking.Confirm")]
    public void Confirm_SetsStatusAndProcessedAt()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), TestUserId);

        // Act
        booking.Confirm();

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt <= DateTime.UtcNow);
    }

    [Fact]
    [Trait("Category", "Booking.Reject")]
    public void Reject_SetsStatusAndProcessedAt()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), TestUserId);

        // Act
        booking.Reject();

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt <= DateTime.UtcNow);
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    public async Task CreateBookingAsync_WhenLimitReached_ThrowsActiveBookingsLimitException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        int limit = BookingService.ActiveBookingsLimit;

        for (var i = 0; i < limit; i++)
            await bookingService.CreateBookingAsync(Guid.NewGuid(), TestUserId);

        // Act & Assert
        await Assert.ThrowsAsync<ActiveBookingsLimitException>(
            () => bookingService.CreateBookingAsync(Guid.NewGuid(), TestUserId));
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    public async Task CreateBookingAsync_WhenAnotherUserLimitReached_CreateBooking()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventId = Guid.NewGuid();
        var secondTestUserId = Guid.NewGuid();

        int limit = BookingService.ActiveBookingsLimit;

        for (var i = 0; i < limit; i++)
            await bookingService.CreateBookingAsync(eventId, TestUserId);

        // Act
        BookingDto booking = await bookingService.CreateBookingAsync(eventId, secondTestUserId);

        // Assert
        Assert.NotNull(booking);
        Assert.Equal(eventId, booking.EventId);
    }
}