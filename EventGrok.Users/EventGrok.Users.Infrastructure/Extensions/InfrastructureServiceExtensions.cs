using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EventGrok.Users.Infrastructure.Data;
using EventGrok.Users.Infrastructure.Repositories;
using EventGrok.Users.Application.Interfaces;
using EventGrok.Users.Infrastructure.Services;
using EventGrok.Users.Infrastructure.Settings;

namespace EventGrok.Users.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UsersDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        
        JwtSettings jwtSettings = new()
        {
            Secret = configuration["JwtSettings:Secret"]!,
            Issuer = configuration["JwtSettings:Issuer"]!,
            Audience = configuration["JwtSettings:Audience"]!,
            LifetimeMinutes = int.Parse(configuration["JwtSettings:LifetimeMinutes"] ?? "60")
        };
        services.AddSingleton(jwtSettings);

        services.AddScoped<ITokenService, JwtTokenGenerator>();

        return services;
    }
}