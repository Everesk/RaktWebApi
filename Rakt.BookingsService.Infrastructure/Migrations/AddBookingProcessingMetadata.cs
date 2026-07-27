using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rakt.BookingsService.Infrastructure.Migrations;

/// <inheritdoc />
[DbContext(typeof(BookingsDbContext))]
[Migration("20260727170000_AddBookingProcessingMetadata")]
public sealed class AddBookingProcessingMetadata : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "bookings",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: DateTimeOffset.UnixEpoch);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ProcessedAt",
            table: "bookings",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CreatedAt",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "ProcessedAt",
            table: "bookings");
    }
}
