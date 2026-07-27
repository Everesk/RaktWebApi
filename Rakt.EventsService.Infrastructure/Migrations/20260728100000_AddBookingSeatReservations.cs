using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rakt.EventsService.Infrastructure.Migrations;

/// <inheritdoc />
[DbContext(typeof(EventsDbContext))]
[Migration("20260728100000_AddBookingSeatReservations")]
public sealed class AddBookingSeatReservations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "booking_seat_reservations",
            columns: table => new
            {
                BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                IsSeatReserved = table.Column<bool>(type: "boolean", nullable: false),
                IsCancelled = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_booking_seat_reservations", reservation => reservation.BookingId);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "booking_seat_reservations");
    }
}
