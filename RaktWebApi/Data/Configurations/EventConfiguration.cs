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
        builder.ToTable("Events");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.StartAt)
            .IsRequired();

        builder.Property(x => x.EndAt)
            .IsRequired();

        builder.Property(x => x.TotalSeats)
            .IsRequired();

        builder.Property(x => x.AvailableSeats)
            .IsRequired();

        builder.HasMany(x => x.Bookings)
            .WithOne(x => x.Event)
            .HasForeignKey(x => x.EventId);
    }
}
