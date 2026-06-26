using EventGrok.ApiTests.Fixtures;
using EventGrok.Application.DTOs;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Net;
using Microsoft.AspNetCore.Http;
using EventGrok.Application.Services;

namespace EventGrok.ApiTests;

public class BookingsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private async Task<HttpClient> GetAuthenticatedClientAsync(string login, string password, string role)
    {
        HttpClient authClient = factory.CreateClient();

        RegisterDto registerDto = new() { Login = login, Password = password, Role = role };
        await authClient.PostAsJsonAsync("auth/register", registerDto);

        LoginDto loginDto = new() { Login = login, Password = password };
        HttpResponseMessage response = await authClient.PostAsJsonAsync("auth/login", loginDto);

        var tokenResponseDto = await response.Content.ReadFromJsonAsync<TokenResponseDto>();

        authClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenResponseDto!.Token);

        return authClient;
    }

    private static CreateEventDto CreateValidEventDto(string title = "Test Event", int totalSeats = 100) =>
        new()
        {
            Title = title,
            StartAt = DateTime.UtcNow.AddHours(1),
            EndAt = DateTime.UtcNow.AddHours(2),
            TotalSeats = totalSeats
        };

    [Fact]
    public async Task BookingFlow_FullCycle_Success()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        HttpClient adminClient = await GetAuthenticatedClientAsync("Admin", "123456", "Admin");
        HttpClient userClient = await GetAuthenticatedClientAsync("User", "123456", "User");

        CreateEventDto createEventDto = CreateValidEventDto();

        // Act & Assert
        HttpResponseMessage createEventResponse = await adminClient.PostAsJsonAsync("events", createEventDto);
        Assert.Equal(HttpStatusCode.Created, createEventResponse.StatusCode);
        var createdEvent = await createEventResponse.Content.ReadFromJsonAsync<EventInfoDto>();
        Assert.NotNull(createdEvent);

        HttpResponseMessage bookingResponse = await userClient.PostAsync($"events/{createdEvent.Id}/book", content: null);
        Assert.Equal(HttpStatusCode.Accepted, bookingResponse.StatusCode);
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingDto>();
        Assert.NotNull(booking);
        Assert.Equal("Pending", booking.Status);

        HttpResponseMessage getBookingResponse = await userClient.GetAsync($"bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.OK, getBookingResponse.StatusCode);
        var retrievedBooking = await getBookingResponse.Content.ReadFromJsonAsync<BookingDto>();
        Assert.NotNull(retrievedBooking);
        Assert.Equal(booking.Id, retrievedBooking.Id);

        HttpResponseMessage cancelResponse = await userClient.DeleteAsync($"bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        HttpResponseMessage getCancelledResponse = await userClient.GetAsync($"bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.OK, getCancelledResponse.StatusCode);
        var cancelledBooking = await getCancelledResponse.Content.ReadFromJsonAsync<BookingDto>();
        Assert.NotNull(cancelledBooking);
        Assert.Equal("Cancelled", cancelledBooking.Status);
    }

    [Fact]
    public async Task Booking_Overbooking_ReturnsConflict()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        HttpClient adminClient = await GetAuthenticatedClientAsync("Admin", "123456", "Admin");
        HttpClient user1Client = await GetAuthenticatedClientAsync("User1", "123456", "User");
        HttpClient user2Client = await GetAuthenticatedClientAsync("User2", "123456", "User");

        CreateEventDto createEventDto = CreateValidEventDto("Mono Event", 1);

        HttpResponseMessage createEventResponse = await adminClient.PostAsJsonAsync("events", createEventDto);
        EventInfoDto? createdEvent = await createEventResponse.Content.ReadFromJsonAsync<EventInfoDto>();
        Assert.NotNull(createdEvent);

        // Act
        Task<HttpResponseMessage> firstBooking = user1Client.PostAsync($"events/{createdEvent.Id}/book", content: null);
        Task<HttpResponseMessage> secondBooking = user2Client.PostAsync($"events/{createdEvent.Id}/book", content: null);

        HttpResponseMessage[] bookingResponses = await Task.WhenAll(firstBooking, secondBooking);

        // Assert
        Assert.Contains(bookingResponses, br => br.StatusCode == HttpStatusCode.Conflict);
        Assert.Contains(bookingResponses, br => br.StatusCode == HttpStatusCode.Accepted);

        HttpResponseMessage bookedEventResponse = await adminClient.GetAsync($"events/{createdEvent.Id}");
        EventInfoDto? bookedEvent = await bookedEventResponse.Content.ReadFromJsonAsync<EventInfoDto>();
        Assert.NotNull(bookedEvent);
        Assert.Equal(0, bookedEvent.AvailableSeats);
    }

    [Fact]
    public async Task Booking_UserLimitReached_ReturnsConflict()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        HttpClient adminClient = await GetAuthenticatedClientAsync("Admin", "123456", "Admin");
        HttpClient userClient = await GetAuthenticatedClientAsync("User", "123456", "User");

        int limit = BookingService.ActiveBookingsLimit;

        for (var i = 1; i <= limit; i++)
        {
            CreateEventDto createEventDto = CreateValidEventDto($"Event {i}");
            HttpResponseMessage response = await adminClient.PostAsJsonAsync("events", createEventDto);
            var createdEvent = await response.Content.ReadFromJsonAsync<EventInfoDto>();

            HttpResponseMessage bookResponse = await userClient.PostAsync($"events/{createdEvent!.Id}/book", content: null);
            Assert.Equal(HttpStatusCode.Accepted, bookResponse.StatusCode);
        }

        CreateEventDto extraEventDto = CreateValidEventDto("Extra Event");
        HttpResponseMessage extraResponse = await adminClient.PostAsJsonAsync("events", extraEventDto);
        var extraEvent = await extraResponse.Content.ReadFromJsonAsync<EventInfoDto>();

        // Act
        HttpResponseMessage limitResponse = await userClient.PostAsync($"events/{extraEvent!.Id}/book", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, limitResponse.StatusCode);
    }

    [Fact]
    public async Task Booking_PastEvent_ReturnsBadRequest()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        HttpClient adminClient = await GetAuthenticatedClientAsync("Admin", "123456", "Admin");
        HttpClient userClient = await GetAuthenticatedClientAsync("User", "123456", "User");

        CreateEventDto createEventDto = new()
        {
            Title = "Past Event",
            StartAt = DateTime.UtcNow.AddHours(-2),
            EndAt = DateTime.UtcNow.AddHours(-1),
            TotalSeats = 100
        };

        HttpResponseMessage createEventResponse = await adminClient.PostAsJsonAsync("events", createEventDto);
        Assert.Equal(HttpStatusCode.Created, createEventResponse.StatusCode);
        var createdEvent = await createEventResponse.Content.ReadFromJsonAsync<EventInfoDto>();
        Assert.NotNull(createdEvent);

        // Act
        HttpResponseMessage bookingResponse = await userClient.PostAsync($"events/{createdEvent.Id}/book", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, bookingResponse.StatusCode);
    }

    [Fact]
    public async Task Booking_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        HttpClient adminClient = await GetAuthenticatedClientAsync("Admin", "123456", "Admin");
        HttpClient unauthClient = factory.CreateClient();

        CreateEventDto createEventDto = CreateValidEventDto();

        HttpResponseMessage createEventResponse = await adminClient.PostAsJsonAsync("events", createEventDto);
        Assert.Equal(HttpStatusCode.Created, createEventResponse.StatusCode);
        var createdEvent = await createEventResponse.Content.ReadFromJsonAsync<EventInfoDto>();
        Assert.NotNull(createdEvent);

        // Act
        HttpResponseMessage bookingResponse = await unauthClient.PostAsync($"events/{createdEvent.Id}/book", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, bookingResponse.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_OnlyOwnerOrAdmin_CanCancel()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        HttpClient adminClient = await GetAuthenticatedClientAsync("Admin", "123456", "Admin");
        HttpClient ownerClient = await GetAuthenticatedClientAsync("Owner", "123456", "User");
        HttpClient otherClient = await GetAuthenticatedClientAsync("Other", "123456", "User");

        CreateEventDto createEventDto = CreateValidEventDto();

        HttpResponseMessage createEventResponse = await adminClient.PostAsJsonAsync("events", createEventDto);
        Assert.Equal(HttpStatusCode.Created, createEventResponse.StatusCode);
        var createdEvent = await createEventResponse.Content.ReadFromJsonAsync<EventInfoDto>();
        Assert.NotNull(createdEvent);

        HttpResponseMessage bookResponse = await ownerClient.PostAsync($"events/{createdEvent.Id}/book", content: null);
        var booking = await bookResponse.Content.ReadFromJsonAsync<BookingDto>();
        Assert.NotNull(booking);

        // Act
        HttpResponseMessage otherCancelResponse = await otherClient.DeleteAsync($"bookings/{booking.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, otherCancelResponse.StatusCode);

        // Act
        HttpResponseMessage adminCancelResponse = await adminClient.DeleteAsync($"bookings/{booking.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, adminCancelResponse.StatusCode);

        HttpResponseMessage getCancelledResponse = await ownerClient.GetAsync($"bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.OK, getCancelledResponse.StatusCode);
        var cancelledBooking = await getCancelledResponse.Content.ReadFromJsonAsync<BookingDto>();
        Assert.NotNull(cancelledBooking);
        Assert.Equal("Cancelled", cancelledBooking.Status);
    }

    [Fact]
    public async Task Booking_NonExistingEvent_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        HttpClient userClient = await GetAuthenticatedClientAsync("User", "123456", "User");
        Guid fakeEventId = Guid.NewGuid();

        // Act
        HttpResponseMessage bookingResponse = await userClient.PostAsync($"events/{fakeEventId}/book", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, bookingResponse.StatusCode);
    }

    [Fact]
    public async Task GetBooking_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        HttpClient userClient = await GetAuthenticatedClientAsync("User", "123456", "User");
        Guid fakeBookingId = Guid.NewGuid();

        // Act
        HttpResponseMessage bookingResponse = await userClient.GetAsync($"bookings/{fakeBookingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, bookingResponse.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        HttpClient userClient = await GetAuthenticatedClientAsync("User", "123456", "User");
        Guid fakeBookingId = Guid.NewGuid();

        // Act
        HttpResponseMessage bookingResponse = await userClient.DeleteAsync($"bookings/{fakeBookingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, bookingResponse.StatusCode);
    }
}