using EventGrok.Services;
using EventGrok.Models;
using EventGrok.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using EventGrok.DataAccess;
using Microsoft.EntityFrameworkCore;

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

        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IEventService, EventService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    private IBookingService GetBookingService() =>
        _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IBookingService>();

    private IEventService GetEventService() =>
        _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IEventService>();

    private static Event CreateValidEvent(string title = "Test Event", int totalSeats = 100) =>
        Event.Create(title, "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), totalSeats);

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
        IBookingService bookingService = GetBookingService();
        IEventService eventService = GetEventService();

        Event eventToBook = await eventService.AddEventAsync(CreateValidEvent());

        // Act
        Booking booking = await bookingService.CreateBookingAsync(eventToBook.Id);

        // Assert
        Assert.NotNull(booking);
        Assert.Equal(eventToBook.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.NotEqual(DateTime.MinValue, booking.CreatedAt);
        Assert.Null(booking.ProcessedAt);
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    [Trait("Data", "Valid")]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_UniqueIds()
    {
        // Arrange
        IBookingService bookingService = GetBookingService();
        IEventService eventService = GetEventService();

        Event eventToBook = await eventService.AddEventAsync(CreateValidEvent());

        // Act
        Booking booking1 = await bookingService.CreateBookingAsync(eventToBook.Id);
        Booking booking2 = await bookingService.CreateBookingAsync(eventToBook.Id);
        Booking booking3 = await bookingService.CreateBookingAsync(eventToBook.Id);

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
        IBookingService bookingService = GetBookingService();
        IEventService eventService = GetEventService();

        Event eventToBook = await eventService.AddEventAsync(CreateValidEvent());
        Booking createdBooking = await bookingService.CreateBookingAsync(eventToBook.Id);

        // Act
        Booking retrievedBooking = await bookingService.GetBookingByIdAsync(createdBooking.Id);

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
        IBookingService bookingService = GetBookingService();
        IEventService eventService = GetEventService();

        Event eventToBook = await eventService.AddEventAsync(CreateValidEvent());
        Booking createdBooking = await bookingService.CreateBookingAsync(eventToBook.Id);

        // Act
        createdBooking.Status = BookingStatus.Confirmed;
        createdBooking.ProcessedAt = DateTime.UtcNow;
        await bookingService.UpdateBookingAsync(createdBooking);

        Booking updatedBooking = await bookingService.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        Assert.Equal(BookingStatus.Confirmed, updatedBooking.Status);
        Assert.NotNull(updatedBooking.ProcessedAt);
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    [Trait("Data", "Invalid")]
    public async Task CreateBookingAsync_NonExistingEvent_ThrowsKeyNotFoundException()
    {
        // Arrange
        IBookingService bookingService = GetBookingService();
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
        IBookingService bookingService = GetBookingService();
        IEventService eventService = GetEventService();

        Event eventToBook = await eventService.AddEventAsync(CreateValidEvent());
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
        IBookingService bookingService = GetBookingService();
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
        IBookingService bookingService = GetBookingService();
        IEventService eventService = GetEventService();

        Event eventToBook = CreateValidEvent("Концерт", totalSeats: 5);
        await eventService.AddEventAsync(eventToBook);

        // Act
        await bookingService.CreateBookingAsync(eventToBook.Id);

        // Assert
        Event eventAfterBooking = await eventService.GetEventByIdAsync(eventToBook.Id);
        Assert.Equal(4, eventAfterBooking.AvailableSeats);
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    public async Task CreateBookingAsync_MultipleUpToLimit_AllSucceedWithUniqueIds()
    {
        // Arrange
        IBookingService bookingService = GetBookingService();
        IEventService eventService = GetEventService();

        Event eventToBook = CreateValidEvent("Фестиваль", totalSeats: 3);
        await eventService.AddEventAsync(eventToBook);

        // Act
        Booking b1 = await bookingService.CreateBookingAsync(eventToBook.Id);
        Booking b2 = await bookingService.CreateBookingAsync(eventToBook.Id);
        Booking b3 = await bookingService.CreateBookingAsync(eventToBook.Id);

        // Assert
        Assert.Equal(3, new[] { b1.Id, b2.Id, b3.Id }.Distinct().Count());

        Event eventAfter = await eventService.GetEventByIdAsync(eventToBook.Id);
        Assert.Equal(0, eventAfter.AvailableSeats);
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    public async Task CreateBookingAsync_NoSeatsAvailable_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        IBookingService bookingService = GetBookingService();
        IEventService eventService = GetEventService();

        Event eventToBook = CreateValidEvent("Аквадискотека", totalSeats: 1);
        await eventService.AddEventAsync(eventToBook);
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
        IEventService eventService = GetEventService();
        Event eventToBook = CreateValidEvent("Выставка кошек", totalSeats: 10);
        await eventService.AddEventAsync(eventToBook);

        int totalAttempts = 10;

        // Act
        IEnumerable<Task<Booking>> tasks = Enumerable.Range(0, totalAttempts)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedBookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                return await scopedBookingService.CreateBookingAsync(eventToBook.Id);
            }));

        Booking[] bookings = await Task.WhenAll(tasks);

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
        IEventService eventService = GetEventService();
        Event eventToBook = CreateValidEvent("Аншлаг", totalSeats: 5);
        await eventService.AddEventAsync(eventToBook);

        int totalAttempts = 20;

        // Act & Assert
        IEnumerable<Task<Booking?>> tasks = Enumerable.Range(0, totalAttempts)
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

        Booking?[] results = await Task.WhenAll(tasks);

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
        IEventService eventService = GetEventService();
        Event eventToBook = CreateValidEvent("Спектакль", totalSeats: 1);
        await eventService.AddEventAsync(eventToBook);

        // Act
        Booking first = await GetBookingService().CreateBookingAsync(eventToBook.Id);

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var eventEntity = await context.Events.FindAsync(eventToBook.Id);
            eventEntity!.ReleaseSeats(1);
            await context.SaveChangesAsync();
        }

        Booking second = await GetBookingService().CreateBookingAsync(eventToBook.Id);

        // Assert
        Assert.NotNull(second);
        Assert.NotEqual(first.Id, second.Id);

        Event eventAfter = await eventService.GetEventByIdAsync(eventToBook.Id);
        Assert.Equal(0, eventAfter.AvailableSeats);
    }
}