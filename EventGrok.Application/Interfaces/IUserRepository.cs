using EventGrok.Domain.Entities;

namespace EventGrok.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByLoginAsync(string login, CancellationToken ct = default);
    Task AddUserAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}