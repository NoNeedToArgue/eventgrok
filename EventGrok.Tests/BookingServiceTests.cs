using EventGrok.Services;
using EventGrok.Models;
using EventGrok.Exceptions;

namespace EventGrok.Tests;

public class BookingServiceTests
{
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    public BookingServiceTests()
    {
        _eventService = new EventService();
        _bookingService = new BookingService(_eventService);
    }

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
        Event eventToBook = _eventService.AddEvent(CreateValidEvent());

        // Act
        Booking booking = await _bookingService.CreateBookingAsync(eventToBook.Id);

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
        Event eventToBook = _eventService.AddEvent(CreateValidEvent());

        // Act
        Booking booking1 = await _bookingService.CreateBookingAsync(eventToBook.Id);
        Booking booking2 = await _bookingService.CreateBookingAsync(eventToBook.Id);
        Booking booking3 = await _bookingService.CreateBookingAsync(eventToBook.Id);

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
        Event eventToBook = _eventService.AddEvent(CreateValidEvent());
        Booking createdBooking = await _bookingService.CreateBookingAsync(eventToBook.Id);

        // Act
        Booking retrievedBooking = await _bookingService.GetBookingByIdAsync(createdBooking.Id);

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
        Event eventToBook = _eventService.AddEvent(CreateValidEvent());
        Booking createdBooking = await _bookingService.CreateBookingAsync(eventToBook.Id);

        // Act
        createdBooking.Status = BookingStatus.Confirmed;
        createdBooking.ProcessedAt = DateTime.UtcNow;
        await _bookingService.UpdateBookingAsync(createdBooking);

        Booking updatedBooking = await _bookingService.GetBookingByIdAsync(createdBooking.Id);

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
        Guid nonExistingEventId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _bookingService.CreateBookingAsync(nonExistingEventId));
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    [Trait("Data", "Invalid")]
    public async Task CreateBookingAsync_DeletedEvent_ThrowsKeyNotFoundException()
    {
        // Arrange
        Event eventToBook = _eventService.AddEvent(CreateValidEvent());
        _eventService.RemoveEvent(eventToBook.Id);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _bookingService.CreateBookingAsync(eventToBook.Id));
    }

    [Fact]
    [Trait("Category", "GetBookingByIdAsync")]
    [Trait("Data", "Invalid")]
    public async Task GetBookingByIdAsync_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        Guid nonExistingBookingId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _bookingService.GetBookingByIdAsync(nonExistingBookingId));
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    public async Task CreateBookingAsync_Success_DecreasesAvailableSeats()
    {
        // Arrange
        Event eventToBook = CreateValidEvent("Концерт", totalSeats: 5);
        _eventService.AddEvent(eventToBook);

        // Act
        await _bookingService.CreateBookingAsync(eventToBook.Id);

        // Assert
        Event eventAfterBooking = _eventService.GetEventById(eventToBook.Id);
        Assert.Equal(4, eventAfterBooking.AvailableSeats);
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    public async Task CreateBookingAsync_MultipleUpToLimit_AllSucceedWithUniqueIds()
    {
        // Arrange
        Event eventToBook = CreateValidEvent("Фестиваль", totalSeats: 3);
        _eventService.AddEvent(eventToBook);

        // Act
        Booking b1 = await _bookingService.CreateBookingAsync(eventToBook.Id);
        Booking b2 = await _bookingService.CreateBookingAsync(eventToBook.Id);
        Booking b3 = await _bookingService.CreateBookingAsync(eventToBook.Id);

        // Assert
        Assert.Equal(3, new[] { b1.Id, b2.Id, b3.Id }.Distinct().Count());
        Assert.Equal(0, _eventService.GetEventById(eventToBook.Id).AvailableSeats);
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    public async Task CreateBookingAsync_NoSeatsAvailable_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        Event eventToBook = CreateValidEvent("Аквадискотека", totalSeats: 1);
        _eventService.AddEvent(eventToBook);
        await _bookingService.CreateBookingAsync(eventToBook.Id);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            () => _bookingService.CreateBookingAsync(eventToBook.Id));
    }

    [Fact]
    [Trait("Category", "CreateBookingAsync")]
    [Trait("Type", "Concurrency")]
    public async Task CreateBookingAsync_ConcurrentRequests_AllIdsUnique()
    {
        // Arrange
        Event eventToBook = CreateValidEvent("Выставка кошек", totalSeats: 10);
        _eventService.AddEvent(eventToBook);

        int totalAttempts = 10;

        // Act
        IEnumerable<Task<Booking>> tasks = Enumerable.Range(0, totalAttempts)
            .Select(_ => Task.Run(() => _bookingService.CreateBookingAsync(eventToBook.Id)));
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
        Event eventToBook = CreateValidEvent("Аншлаг", totalSeats: 5);
        _eventService.AddEvent(eventToBook);

        int totalAttempts = 20;

        // Act & Assert
        IEnumerable<Task<Booking?>> tasks = Enumerable.Range(0, totalAttempts)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    return await _bookingService.CreateBookingAsync(eventToBook.Id);
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
        Event eventToBook = CreateValidEvent("Спектакль", totalSeats: 1);
        _eventService.AddEvent(eventToBook);

        // Act
        Booking first = await _bookingService.CreateBookingAsync(eventToBook.Id);

        eventToBook.ReleaseSeats(1);

        Booking second = await _bookingService.CreateBookingAsync(eventToBook.Id);

        // Assert
        Assert.NotNull(second);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(0, eventToBook.AvailableSeats);
    }
}