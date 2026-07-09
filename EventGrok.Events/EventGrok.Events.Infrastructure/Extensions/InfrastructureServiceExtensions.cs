using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EventGrok.Events.Infrastructure.Data;
using EventGrok.Events.Infrastructure.Repositories;
using EventGrok.Events.Application.Interfaces;
using EventGrok.Events.Infrastructure.Settings;

namespace EventGrok.Events.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEventRepository, EventRepository>();

        JwtSettings jwtSettings = new()
        {
            Secret = configuration["JwtSettings:Secret"]!,
            Issuer = configuration["JwtSettings:Issuer"]!,
            Audience = configuration["JwtSettings:Audience"]!
        };
        services.AddSingleton(jwtSettings);

        return services;
    }
}