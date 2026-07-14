using EventGrok.Bookings.ApiTests.Fixtures;
using EventGrok.Bookings.Application.DTOs;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Net;
using Microsoft.AspNetCore.Http;
using EventGrok.Bookings.Application.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EventGrok.Bookings.ApiTests;

public class BookingsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static string GenerateTestToken(Guid userId, string role)
    {
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, "testuser")
        ];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("NotSoSecretKey_ONLY_FOR_DEMONSTRATION!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "EventGrok",
            audience: "EventGrokClients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private HttpClient CreateAuthenticatedClient(Guid userId, string role = "User")
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateTestToken(userId, role));
        return client;
    }

    [Fact]
    public async Task BookingFlow_FullCycle_Success()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        HttpClient client = CreateAuthenticatedClient(userId);

        var eventId = Guid.NewGuid();
        var createBookingDto = new CreateBookingDto(eventId);

        // Act & Assert
        HttpResponseMessage bookingResponse = await client.PostAsJsonAsync("bookings", createBookingDto);
        Assert.Equal(HttpStatusCode.Accepted, bookingResponse.StatusCode);
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingDto>();
        Assert.NotNull(booking);
        Assert.Equal("Pending", booking.Status);

        HttpResponseMessage getBookingResponse = await client.GetAsync($"bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.OK, getBookingResponse.StatusCode);
        var retrievedBooking = await getBookingResponse.Content.ReadFromJsonAsync<BookingDto>();
        Assert.NotNull(retrievedBooking);
        Assert.Equal(booking.Id, retrievedBooking.Id);

        HttpResponseMessage cancelResponse = await client.DeleteAsync($"bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        HttpResponseMessage getCancelledResponse = await client.GetAsync($"bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.OK, getCancelledResponse.StatusCode);
        var cancelledBooking = await getCancelledResponse.Content.ReadFromJsonAsync<BookingDto>();
        Assert.NotNull(cancelledBooking);
        Assert.Equal("Cancelled", cancelledBooking.Status);
    }

    [Fact]
    public async Task Booking_UserLimitReached_ReturnsConflict()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        HttpClient client = CreateAuthenticatedClient(userId);

        int limit = BookingService.ActiveBookingsLimit;

        for (var i = 1; i <= limit; i++)
        {
            var eventId = Guid.NewGuid();
            HttpResponseMessage bookResponse = await client.PostAsJsonAsync("bookings", new CreateBookingDto(eventId));
            Assert.Equal(HttpStatusCode.Accepted, bookResponse.StatusCode);
        }

        var extraEventId = Guid.NewGuid();

        // Act
        HttpResponseMessage limitResponse = await client.PostAsJsonAsync("bookings", new CreateBookingDto(extraEventId));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, limitResponse.StatusCode);
    }

    [Fact]
    public async Task Booking_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        HttpClient unauthClient = factory.CreateClient();

        var eventId = Guid.NewGuid();
        var createBookingDto = new CreateBookingDto(eventId);

        // Act
        HttpResponseMessage bookingResponse = await unauthClient.PostAsJsonAsync("bookings", createBookingDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, bookingResponse.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_OnlyOwnerOrAdmin_CanCancel()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        HttpClient ownerClient = CreateAuthenticatedClient(ownerId);
        HttpClient otherClient = CreateAuthenticatedClient(otherId);
        HttpClient adminClient = CreateAuthenticatedClient(adminId, "Admin");

        var eventId = Guid.NewGuid();
        var createBookingDto = new CreateBookingDto(eventId);

        HttpResponseMessage bookResponse = await ownerClient.PostAsJsonAsync("bookings", new CreateBookingDto(eventId));
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
    public async Task GetBooking_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        HttpClient client = CreateAuthenticatedClient(userId);
        
        Guid fakeBookingId = Guid.NewGuid();

        // Act
        HttpResponseMessage bookingResponse = await client.GetAsync($"bookings/{fakeBookingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, bookingResponse.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        HttpClient client = CreateAuthenticatedClient(userId);

        Guid fakeBookingId = Guid.NewGuid();

        // Act
        HttpResponseMessage bookingResponse = await client.DeleteAsync($"bookings/{fakeBookingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, bookingResponse.StatusCode);
    }
}