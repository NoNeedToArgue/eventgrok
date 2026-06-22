using System.Security.Cryptography;
using System.Text;
using EventGrok.Application.Interfaces;

namespace EventGrok.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public bool VerifyPassword(string password, string hash) =>
        HashPassword(password) == hash;
}