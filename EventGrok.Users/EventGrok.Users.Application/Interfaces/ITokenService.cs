using EventGrok.Users.Application.DTOs;
using EventGrok.Users.Domain.Entities;

namespace EventGrok.Users.Application.Interfaces;

public interface ITokenService
{
    TokenResponseDto GenerateToken(User user);
}