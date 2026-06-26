using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EventGrok.Infrastructure.Data;
using EventGrok.Infrastructure.Repositories;
using EventGrok.Application.Interfaces;
using EventGrok.Infrastructure.Services;
using EventGrok.Infrastructure.Settings;

namespace EventGrok.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
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