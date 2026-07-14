using EventGrok.Users.Domain.Entities;

namespace EventGrok.Users.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByLoginAsync(string login, CancellationToken ct = default);
    Task AddUserAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}