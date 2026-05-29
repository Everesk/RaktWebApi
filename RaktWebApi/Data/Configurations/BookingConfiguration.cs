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
        builder.ToTable("Bookings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.EventId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ProcessedAt);

        builder.HasOne(x => x.Event)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.EventId);
    }
}
