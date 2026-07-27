using Microsoft.EntityFrameworkCore;
using Rakt.BookingsService.Application;
using Rakt.BookingsService.Domain;
namespace Rakt.BookingsService.Infrastructure;
/// <summary>Репозиторий броней EF Core.</summary>
public sealed class BookingRepository(BookingsDbContext db) : IBookingRepository
{
    public Task<Booking?> GetAsync(Guid id, CancellationToken ct = default) => db.Bookings.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task AddAsync(Booking booking, CancellationToken ct = default) => db.Bookings.AddAsync(booking, ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
