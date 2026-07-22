using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EventGrok.Events.Infrastructure.Data;
using EventGrok.Events.Infrastructure.Repositories;
using EventGrok.Events.Application.Interfaces;
using EventGrok.Events.Infrastructure.Settings;
using EventGrok.Events.Infrastructure.Kafka;
using StackExchange.Redis;
using EventGrok.Events.Infrastructure.Cache;

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

        KafkaSettings kafkaSettings = new()
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]!,
            ConsumerGroup = configuration["Kafka:ConsumerGroup"]!
        };
        services.AddSingleton(kafkaSettings);

        services.AddHostedService<TopicInitializerService>();

        services.AddHostedService<BookingConfirmedConsumer>();

        string redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var config = ConfigurationOptions.Parse(redisConnectionString);
            config.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(config);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}