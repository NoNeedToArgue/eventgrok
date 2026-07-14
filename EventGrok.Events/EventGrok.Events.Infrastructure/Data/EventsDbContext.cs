using EventGrok.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventGrok.Events.Infrastructure.Data;

public class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);
    }
}