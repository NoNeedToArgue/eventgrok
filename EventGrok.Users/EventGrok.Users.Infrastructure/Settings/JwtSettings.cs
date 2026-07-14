namespace EventGrok.Users.Infrastructure.Settings;

public class JwtSettings
{
    public required string Secret { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int LifetimeMinutes { get; set; } = 60;
}