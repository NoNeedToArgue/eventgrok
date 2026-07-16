using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Testcontainers.PostgreSql;
using EventGrok.Events.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EventGrok.Events.Infrastructure.Kafka;

namespace EventGrok.Events.ApiTests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public string ConnectionString => _postgres.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

            ServiceDescriptor? descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<EventsDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            List<ServiceDescriptor> kafkaServices =
            [
                .. services
                    .Where(d =>
                        d.ImplementationType == typeof(BookingConfirmedConsumer) ||
                        d.ImplementationType == typeof(TopicInitializerService))
            ];


            foreach (ServiceDescriptor serviceDescriptor in kafkaServices)
            {
                services.Remove(serviceDescriptor);
            }

            services.AddDbContext<EventsDbContext>(options =>
                options.UseNpgsql(ConnectionString));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        await context.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE events RESTART IDENTITY CASCADE");
    }
}