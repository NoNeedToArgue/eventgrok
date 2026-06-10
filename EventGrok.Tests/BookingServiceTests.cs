using EventGrok.Application.Services;
using EventGrok.Application.Interfaces;
using EventGrok.Application.DTOs;
using EventGrok.Domain.Entities;
using EventGrok.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using EventGrok.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EventGrok.Infrastructure.Repositories;

namespace EventGrok.Tests;

public class BookingServiceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;

    public BookingServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventRepository, EventRepository>();

        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IEventService, EventService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    private static CreateEventDto CreateValidEventDto(string title = "Test Event", int totalSeats = 100) =>
        new() { Title = title, StartAt = DateTime.UtcNow.AddHours(1), EndAt = DateTime.UtcNow.AddHours(2), TotalSeats = totalSeats };

    private static Booking CreateValidBooking(Guid eventId) => new()
    {
        Id = Guid.NewGuid(),
        EventId = eventId,
        Status = BookingStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    [Trait("Data", "Valid")]
    public async Task CreateBookingAsync_ExistingEvent_ReturnsBookingWithPendingStatus()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        EventInfoDto eventToBook = await eventService.CreateEventAsync(CreateValidEventDto());

        // Act
        BookingDto booking = await bookingService.CreateBookingAsync(eventToBook.Id);

        // Assert
        Assert.NotNull(booking);
        Assert.Equal(eventToBook.Id, booking.EventId);
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
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        EventInfoDto eventToBook = await eventService.CreateEventAsync(CreateValidEventDto());

        // Act
        BookingDto booking1 = await bookingService.CreateBookingAsync(eventToBook.Id);
        BookingDto booking2 = await bookingService.CreateBookingAsync(eventToBook.Id);
        BookingDto booking3 = await bookingService.CreateBookingAsync(eventToBook.Id);

        // Assert
        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.NotEqual(booking2.Id, booking3.Id);
        Assert.NotEqual(booking1.Id, booking3.Id);
        Assert.Equal(eventToBook.Id, booking1.EventId);
        Assert.Equal(eventToBook.Id, booking2.EventId);
        Assert.Equal(eventToBook.Id, booking3.EventId);
    }

    [Fact]
    [Trait("Category", "GetBookingByIdAsync")]
    [Trait("Data", "Valid")]
    public async Task GetBookingByIdAsync_ExistingId_ReturnsCorrectBooking()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        EventInfoDto eventToBook = await eventService.CreateEventAsync(CreateValidEventDto());
        BookingDto createdBooking = await bookingService.CreateBookingAsync(eventToBook.Id);

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
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        EventInfoDto eventToBook = await eventService.CreateEventAsync(CreateValidEventDto());
        BookingDto createdBooking = await bookingService.CreateBookingAsync(eventToBook.Id);

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
    [Trait("Category", "CreateBookingAsync")]
    [Trait("Data", "Invalid")]
    public async Task CreateBookingAsync_NonExistingEvent_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        Guid nonExistingEventId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => bookingService.CreateBookingAsync(nonExistingEventId));
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    [Trait("Data", "Invalid")]
    public async Task CreateBookingAsync_DeletedEvent_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        EventInfoDto eventToBook = await eventService.CreateEventAsync(CreateValidEventDto());
        await eventService.RemoveEventAsync(eventToBook.Id);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => bookingService.CreateBookingAsync(eventToBook.Id));
    }

    [Fact]
    [Trait("Category", "GetBookingByIdAsync")]
    [Trait("Data", "Invalid")]
    public async Task GetBookingByIdAsync_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        Guid nonExistingBookingId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => bookingService.GetBookingByIdAsync(nonExistingBookingId));
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    public async Task CreateBookingAsync_Success_DecreasesAvailableSeats()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        CreateEventDto eventToBookDto = CreateValidEventDto("Концерт", totalSeats: 5);
        EventInfoDto eventToBook = await eventService.CreateEventAsync(eventToBookDto);

        // Act
        await bookingService.CreateBookingAsync(eventToBook.Id);

        // Assert
        EventInfoDto eventAfterBooking = await eventService.GetEventByIdAsync(eventToBook.Id);
        Assert.Equal(4, eventAfterBooking.AvailableSeats);
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    public async Task CreateBookingAsync_MultipleUpToLimit_AllSucceedWithUniqueIds()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        CreateEventDto eventToBookDto = CreateValidEventDto("Фестиваль", totalSeats: 3);
        EventInfoDto eventToBook = await eventService.CreateEventAsync(eventToBookDto);

        // Act
        BookingDto b1 = await bookingService.CreateBookingAsync(eventToBook.Id);
        BookingDto b2 = await bookingService.CreateBookingAsync(eventToBook.Id);
        BookingDto b3 = await bookingService.CreateBookingAsync(eventToBook.Id);

        // Assert
        Assert.Equal(3, new[] { b1.Id, b2.Id, b3.Id }.Distinct().Count());

        EventInfoDto eventAfter = await eventService.GetEventByIdAsync(eventToBook.Id);
        Assert.Equal(0, eventAfter.AvailableSeats);
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    public async Task CreateBookingAsync_NoSeatsAvailable_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        CreateEventDto eventToBookDto = CreateValidEventDto("Аквадискотека", totalSeats: 1);
        EventInfoDto eventToBook = await eventService.CreateEventAsync(eventToBookDto);
        await bookingService.CreateBookingAsync(eventToBook.Id);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            () => bookingService.CreateBookingAsync(eventToBook.Id));
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    [Trait("Type", "Concurrency")]
    public async Task CreateBookingAsync_ConcurrentRequests_AllIdsUnique()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        CreateEventDto eventToBookDto = CreateValidEventDto("Выставка кошек", totalSeats: 10);
        EventInfoDto eventToBook = await eventService.CreateEventAsync(eventToBookDto);

        int totalAttempts = 10;

        // Act
        IEnumerable<Task<BookingDto>> tasks = Enumerable.Range(0, totalAttempts)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedBookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                return await scopedBookingService.CreateBookingAsync(eventToBook.Id);
            }));

        BookingDto[] bookings = await Task.WhenAll(tasks);

        // Assert
        List<Guid> ids = [.. bookings.Select(b => b.Id)];
        Assert.Equal(totalAttempts, ids.Distinct().Count());
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    [Trait("Type", "Concurrency")]
    public async Task CreateBookingAsync_ConcurrentRequests_ExactSuccessCount()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        CreateEventDto eventToBookDto = CreateValidEventDto("Аншлаг", totalSeats: 5);
        EventInfoDto eventToBook = await eventService.CreateEventAsync(eventToBookDto);

        int totalAttempts = 20;

        // Act & Assert
        IEnumerable<Task<BookingDto?>> tasks = Enumerable.Range(0, totalAttempts)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedBookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                try
                {
                    return await scopedBookingService.CreateBookingAsync(eventToBook.Id);
                }
                catch (NoAvailableSeatsException)
                {
                    return null;
                }
            }));

        BookingDto?[] results = await Task.WhenAll(tasks);

        // Assert
        int successCount = results.Count(booking => booking != null);
        int exceptionCount = results.Count(booking => booking == null);

        Assert.Equal(5, successCount);
        Assert.Equal(15, exceptionCount);
    }

    [Fact]
    [Trait("Category", "Booking.Confirm")]
    public void Confirm_SetsStatusAndProcessedAt()
    {
        // Arrange
        Booking booking = CreateValidBooking(Guid.NewGuid());

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
        Booking booking = CreateValidBooking(Guid.NewGuid());

        // Act
        booking.Reject();

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt <= DateTime.UtcNow);
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    public async Task CreateBookingAsync_AfterSeatRelease_CanBookAgain()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        IEventService eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        CreateEventDto eventToBookDto = CreateValidEventDto("Спектакль", totalSeats: 1);
        EventInfoDto eventToBook = await eventService.CreateEventAsync(eventToBookDto);

        // Act
        BookingDto first = await bookingService.CreateBookingAsync(eventToBook.Id);

        var eventEntity = await context.Events.FindAsync(eventToBook.Id);
        eventEntity!.ReleaseSeats(1);
        await context.SaveChangesAsync();

        BookingDto second = await bookingService.CreateBookingAsync(eventToBook.Id);

        // Assert
        Assert.NotNull(second);
        Assert.NotEqual(first.Id, second.Id);

        EventInfoDto eventAfter = await eventService.GetEventByIdAsync(eventToBook.Id);
        Assert.Equal(0, eventAfter.AvailableSeats);
    }
}