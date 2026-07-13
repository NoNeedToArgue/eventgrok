using Microsoft.EntityFrameworkCore;
using EventGrok.Users.Infrastructure.Data;
using EventGrok.Users.Infrastructure.Repositories;
using EventGrok.Users.Domain.Entities;
using EventGrok.Users.IntegrationTests.Fixtures;
using EventGrok.Users.IntegrationTests.CollectionDefinitions;

namespace EventGrok.Users.IntegrationTests;

[Collection(nameof(PostgresTestCollection))]
public class UserRepositoryTests(PostgresContainerFixture fixture)
{
    private async Task<UsersDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        var context = new UsersDbContext(options);
        await context.Database.MigrateAsync();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = await CreateContextAsync();
        
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE users RESTART IDENTITY CASCADE");
    }

    private static User CreateValidUser() =>
        User.Create("TestUser", "TestHash", Role.User);

    [Fact]
    public async Task AddUserAsync_ValidUser_SavesToDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var repo = new UserRepository(context);

        User newUser = CreateValidUser();

        // Act
        await repo.AddUserAsync(newUser);
        await repo.SaveChangesAsync();

        // Assert
        User? savedUser = await repo.GetUserByLoginAsync(newUser.Login);
        Assert.NotNull(savedUser);
        Assert.Equal(newUser.Id, savedUser.Id);
        Assert.Equal(newUser.Login, savedUser.Login);
        Assert.Equal(newUser.PasswordHash, savedUser.PasswordHash);
        Assert.Equal(Role.User, savedUser.Role);
    }

    [Fact]
    public async Task AddUserAsync_DuplicateLogin_ThrowsDbUpdateException()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContextAsync();
        var repo = new UserRepository(context);

        User testUser1 = CreateValidUser();
        User testUser2 = CreateValidUser();

        await repo.AddUserAsync(testUser1);
        await repo.SaveChangesAsync();

        // Act & Assert
        await repo.AddUserAsync(testUser2);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            repo.SaveChangesAsync());
    }
}