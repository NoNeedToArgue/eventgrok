namespace EventGrok.Users.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public required string Login { get; set; }

    public required string PasswordHash { get; set; }

    public Role Role { get; set; } = Role.User;

    public static User Create(string login, string passwordHash, Role role = Role.User) =>
        new()
        {
            Id = Guid.NewGuid(),
            Login = login.Trim(),
            PasswordHash = passwordHash,
            Role = role
        };
}