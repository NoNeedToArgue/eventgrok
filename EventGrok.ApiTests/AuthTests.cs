using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EventGrok.ApiTests.Fixtures;
using EventGrok.Application.DTOs;
using System.Net.Http.Json;
using System.Net;
using EventGrok.Infrastructure.Data;
using EventGrok.Domain.Entities;

namespace EventGrok.ApiTests;

public class AuthTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    public static TheoryData<string, string, string> ValidRegisterData => new()
    {
        { "ValidUser", "123456", "User" },
        { "Admin", "password123456", "Admin" },
        { new string('a', 50), "123456", "User" },
        { "AnotherdUser", new string('a', 100), "Admin" }
    };

    [Theory]
    [MemberData(nameof(ValidRegisterData))]
    public async Task Register_ValidData_ReturnsNoContent(string login, string password, string role)
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        RegisterDto request = new() { Login = login, Password = password, Role = role };

        // Act
        var response = await _client.PostAsJsonAsync("auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("TestUser", "123456", "SuperAdmin")]
    [InlineData("TestUser", "123456", "")]
    public async Task Register_UnexpectedRole_UsesDefaultUser(string login, string password, string role)
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        RegisterDto request = new() { Login = login, Password = password, Role = role };

        // Act
        var response = await _client.PostAsJsonAsync("auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        User user = await context.Users.SingleAsync(u => u.Login == login);
        Assert.Equal(Role.User, user.Role);
    }

    public static TheoryData<string, string> InvalidRegisterData => new()
    {
        { "Ab", "123456" },
        { new string('a', 51), "123456" },
        { "ValidUser", "12345" },
        { "ValidUser", new string('a', 101) }
    };

    [Theory]
    [MemberData(nameof(InvalidRegisterData))]
    public async Task Register_InvalidData_ReturnsBadRequest(string login, string password)
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        RegisterDto request = new() { Login = login, Password = password };

        // Act
        var response = await _client.PostAsJsonAsync("auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateLogin_ReturnsConflict()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        RegisterDto request = new() { Login = "Duplicate", Password = "123456" };

        await _client.PostAsJsonAsync("auth/register", request);

        // Act
        var response = await _client.PostAsJsonAsync("auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("NonExistent", "123456")]
    [InlineData("ValidUser", "WrongPassword")]
    public async Task Login_InvalidCredentials_ReturnsNotFound(string login, string password)
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        if (login == "ValidUser")
        {
            RegisterDto registerDto = new() { Login = login, Password = "123456" };
            await _client.PostAsJsonAsync("auth/register", registerDto);
        }

        LoginDto loginDto = new() { Login = login, Password = password };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
