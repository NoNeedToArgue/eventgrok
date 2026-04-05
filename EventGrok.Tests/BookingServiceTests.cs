using EventGrok.Services;
using EventGrok.Models;

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

    private static Event CreateValidEvent(string title = "Test Event") => new()
    {
        Title = title,
        Description = "Description",
        StartAt = DateTime.UtcNow.AddHours(1),
        EndAt = DateTime.UtcNow.AddHours(2)
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
}