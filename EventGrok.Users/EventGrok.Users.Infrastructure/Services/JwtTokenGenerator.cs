using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventGrok.Users.Application.DTOs;
using EventGrok.Users.Application.Interfaces;
using EventGrok.Users.Domain.Entities;
using EventGrok.Users.Infrastructure.Settings;
using Microsoft.IdentityModel.Tokens;

namespace EventGrok.Users.Infrastructure.Services;

public class JwtTokenGenerator(JwtSettings settings) : ITokenService
{
    public TokenResponseDto GenerateToken(User user)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Login),
            new(ClaimTypes.Role, user.Role.ToString())
        ];

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(settings.Secret));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        DateTime expires = DateTime.UtcNow.AddMinutes(settings.LifetimeMinutes);

        JwtSecurityToken token = new(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new TokenResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expires
        };
    }
}