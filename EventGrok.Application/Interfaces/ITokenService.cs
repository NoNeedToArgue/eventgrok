using EventGrok.Application.DTOs;
using EventGrok.Domain.Entities;

namespace EventGrok.Application.Interfaces;

public interface ITokenService
{
    TokenResponseDto GenerateToken(User user);
}