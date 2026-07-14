using EventGrok.Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventGrok.Bookings.Infrastructure.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.EventId).IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.CreatedAt).IsRequired();
    }
}