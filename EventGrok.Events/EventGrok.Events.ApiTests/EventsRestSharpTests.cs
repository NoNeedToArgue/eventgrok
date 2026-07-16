using System.Net;
using EventGrok.Events.ApiTests.Fixtures;
using EventGrok.Events.Application.DTOs;
using RestSharp;

namespace EventGrok.Events.ApiTests;

public class EventsRestSharpTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private HttpClient CreateAuthenticatedClient(Guid userId, string role = "User")
    {
        HttpClient client = factory.CreateClient();

        client.DefaultRequestHeaders.Remove("X-Test-UserId");
        client.DefaultRequestHeaders.Remove("X-Test-Role");

        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", role);

        return client;
    }
    
    private static CreateEventDto CreateValidEventDto(string title = "Test Event", int totalSeats = 100) => 
        new()
        {
            Title = title, 
            StartAt = DateTime.UtcNow.AddHours(1), 
            EndAt = DateTime.UtcNow.AddHours(2), 
            TotalSeats = totalSeats 
        };

    public static TheoryData<string, string, DateTime, DateTime, int> ValidCreateEventData => new()
    {
        { "Test Event", "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), 100 },
        { "Tes", new string('a', 500), DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), 1 },
        { new string('a', 100), "", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), int.MaxValue },
    };

    [Theory]
    [MemberData(nameof(ValidCreateEventData))]
    public async Task CreateEvent_AdminValidData_ReturnsCreated(
        string title,
        string description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats)
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        RestClient adminClient = new(CreateAuthenticatedClient(Guid.NewGuid(), "Admin"));

        RestRequest request = new("events", Method.Post);
        request.AddJsonBody(new CreateEventDto
            {
                Title = title,
                Description = description,
                StartAt = startAt,
                EndAt = endAt,
                TotalSeats = totalSeats
            });

        // Act
        RestResponse<EventInfoDto> response = await adminClient.ExecuteAsync<EventInfoDto>(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Data);
        Assert.Equal(title, response.Data.Title);

        HeaderParameter? locationHeader = response.Headers?.FirstOrDefault(h => h.Name == "Location");
        Assert.NotNull(locationHeader);
        string? location = locationHeader.Value.ToString();
        Assert.EndsWith($"/{response.Data.Id}", location);
    }

    public static TheoryData<string?, string, DateTime, DateTime, int> InvalidCreateEventData => new()
    {
        { "Te", "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), 100 },
        { new string('a', 101), "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), 100 },
        { "Test Event", new string('a', 501), DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), 100 },
        { "Test Event", "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), 0 },
        { null, "Description", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), 100 }
    };

    [Theory]
    [MemberData(nameof(InvalidCreateEventData))]
    public async Task CreateEvent_AdminInvalidData_ReturnsBadRequest(
        string? title,
        string description,
        DateTime startAt,
        DateTime endAd,
        int totalSeats)
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        RestClient adminClient = new(CreateAuthenticatedClient(Guid.NewGuid(), "Admin"));

        RestRequest request = new("events", Method.Post);
        request.AddJsonBody(new CreateEventDto
            {
                Title = title,
                Description = description,
                StartAt = startAt,
                EndAt = endAd,
                TotalSeats = totalSeats
            });

        // Act
        RestResponse response = await adminClient.ExecuteAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.ContentType);
    }

    [Fact]
    public async Task CreateEvent_User_ReturnsForbidden()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        RestClient userClient = new(CreateAuthenticatedClient(Guid.NewGuid(), "User"));

        RestRequest request = new("events", Method.Post);
        request.AddJsonBody(CreateValidEventDto());

        // Act
        RestResponse response = await userClient.ExecuteAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_UnauthorizedClient_ReturnsUnauthorized()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        RestClient unauthClient = new(factory.CreateClient());

        RestRequest request = new("events", Method.Post);
        request.AddJsonBody(CreateValidEventDto());

        // Act
        RestResponse response = await unauthClient.ExecuteAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
