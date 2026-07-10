using EventGrok.Bookings.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventGrok.Bookings.Infrastructure.Extensions;

public static class DatabaseMigrationExtensions
{
    public static void MigrateDatabase(this IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        BookingsDbContext context = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        context.Database.Migrate();
    }
}