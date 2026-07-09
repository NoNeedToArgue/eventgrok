namespace EventGrok.Users.Application.DTOs;

public class TokenResponseDto
{
    public required string Token { get; set; }
    public required DateTime ExpiresAt { get; set; }
}