using EventGrok.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventGrok.Events.Infrastructure.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description).HasMaxLength(500);

        builder.Property(e => e.StartAt).IsRequired();

        builder.Property(e => e.EndAt).IsRequired();

        builder.Property(e => e.TotalSeats).IsRequired();
    }
}