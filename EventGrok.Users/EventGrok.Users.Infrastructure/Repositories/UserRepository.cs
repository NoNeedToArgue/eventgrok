using EventGrok.Users.Application.Interfaces;
using EventGrok.Users.Domain.Entities;
using EventGrok.Users.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventGrok.Users.Infrastructure.Repositories;

public class UserRepository(UsersDbContext context) : IUserRepository
{
    public async Task<User?> GetUserByLoginAsync(string login, CancellationToken ct = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Login == login, ct);

    public async Task AddUserAsync(User user, CancellationToken ct = default) =>
        await context.Users.AddAsync(user, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);
}