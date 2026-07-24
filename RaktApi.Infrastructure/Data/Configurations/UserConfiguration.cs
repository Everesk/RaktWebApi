using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaktApi.Domain;

namespace RaktApi.Infrastructure.Data.Configurations;

/// <summary>Конфигурация сущности пользователя.</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id).HasName("pk_users");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Login).HasColumnName("login").IsRequired().HasMaxLength(100);
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(64);
        builder.Property(x => x.Role).HasColumnName("role").IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(x => x.Login).IsUnique().HasDatabaseName("ux_users_login");

        builder.HasMany(x => x.Bookings).WithOne(x => x.User).HasForeignKey(x => x.UserId);
    }
}
