using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventGrok.Application.DTOs;
using EventGrok.Application.Interfaces;
using EventGrok.Domain.Entities;
using EventGrok.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventGrok.Infrastructure.Services;

public class JwtTokenGenerator(IOptions<JwtSettings> options) : ITokenService
{
    private readonly JwtSettings _settings = options.Value;

    public TokenResponseDto GenerateToken(User user)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Login),
            new(ClaimTypes.Role, user.Role.ToString())
        ];

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_settings.Secret));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        DateTime expires = DateTime.UtcNow.AddMinutes(_settings.LifetimeMinutes);

        JwtSecurityToken token = new(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
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