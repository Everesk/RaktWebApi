using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaktWebApi.Models;

namespace RaktWebApi.Data.Configurations;

/// <summary>
/// Конфигурация сущности бронирования.
/// </summary>
internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    /// <summary>
    /// Настраивает параметры хранения сущности бронирования в базе данных.
    /// </summary>
    /// <param name="builder">Построитель конфигурации сущности.</param>
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(x => x.Id).HasName("pk_bookings");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");

        builder.HasOne(x => x.Event)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.EventId)
            .HasConstraintName("fk_bookings_events_event_id");
    }
}
