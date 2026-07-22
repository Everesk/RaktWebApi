using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaktWebApi.Models;

namespace RaktWebApi.Data.Configurations;

/// <summary>
/// Конфигурация сущности события.
/// </summary>
internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    /// <summary>
    /// Настраивает параметры хранения сущности события в базе данных.
    /// </summary>
    /// <param name="builder">Построитель конфигурации сущности.</param>
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(x => x.Id).HasName("pk_events");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(x => x.StartAt)
            .HasColumnName("start_at")
            .IsRequired();

        builder.Property(x => x.EndAt)
            .HasColumnName("end_at")
            .IsRequired();

        builder.Property(x => x.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(x => x.AvailableSeats)
            .HasColumnName("available_seats")
            .IsRequired();

        builder.HasMany(x => x.Bookings)
            .WithOne(x => x.Event)
            .HasForeignKey(x => x.EventId);
    }
}
