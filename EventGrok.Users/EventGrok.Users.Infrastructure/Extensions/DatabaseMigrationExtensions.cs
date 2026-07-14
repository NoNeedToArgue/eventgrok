using EventGrok.Users.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventGrok.Users.Infrastructure.Extensions;

public static class DatabaseMigrationExtensions
{
    public static void MigrateDatabase(this IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        UsersDbContext context = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        context.Database.Migrate();
    }
}