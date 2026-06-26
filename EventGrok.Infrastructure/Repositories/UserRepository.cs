using EventGrok.Application.Interfaces;
using EventGrok.Domain.Entities;
using EventGrok.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventGrok.Infrastructure.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetUserByLoginAsync(string login, CancellationToken ct = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Login == login, ct);

    public async Task AddUserAsync(User user, CancellationToken ct = default) =>
        await context.Users.AddAsync(user, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);
}