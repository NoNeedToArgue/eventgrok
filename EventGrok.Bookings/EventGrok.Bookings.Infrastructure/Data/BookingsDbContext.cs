using EventGrok.Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventGrok.Bookings.Infrastructure.Data;

public class BookingsDbContext(DbContextOptions<BookingsDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingsDbContext).Assembly);
    }
}