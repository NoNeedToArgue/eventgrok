using EventGrok.Events.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventGrok.Events.Infrastructure.Extensions;

public static class DatabaseMigrationExtensions
{
    public static void MigrateDatabase(this IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        EventsDbContext context = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        context.Database.Migrate();
    }
}