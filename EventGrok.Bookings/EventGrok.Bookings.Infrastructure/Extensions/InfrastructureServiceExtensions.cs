using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EventGrok.Bookings.Infrastructure.Data;
using EventGrok.Bookings.Infrastructure.Repositories;
using EventGrok.Bookings.Application.Interfaces;
using EventGrok.Bookings.Infrastructure.Settings;

namespace EventGrok.Bookings.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BookingsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IBookingRepository, BookingRepository>();
        
        JwtSettings jwtSettings = new()
        {
            Secret = configuration["JwtSettings:Secret"]!,
            Issuer = configuration["JwtSettings:Issuer"]!,
            Audience = configuration["JwtSettings:Audience"]!,
        };
        services.AddSingleton(jwtSettings);

        return services;
    }
}