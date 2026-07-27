using Microsoft.EntityFrameworkCore;
using Rakt.BookingsService.Application;
using Rakt.BookingsService.Domain;
namespace Rakt.BookingsService.Infrastructure;
/// <summary>Репозиторий броней EF Core.</summary>
public sealed class BookingRepository(BookingsDbContext db) : IBookingRepository
{
    /// <summary>
    /// Возвращает отслеживаемую бронь по идентификатору.
    /// </summary>
    public Task<Booking?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return db.Bookings.SingleOrDefaultAsync(booking => booking.Id == id, ct);
    }

    /// <summary>
    /// Возвращает идентификаторы броней, которые ожидают подтверждения.
    /// </summary>
    public async Task<IReadOnlyCollection<Guid>> GetPendingIdsAsync(CancellationToken ct = default)
    {
        return await db.Bookings
            .AsNoTracking()
            .Where(booking => booking.Status == BookingStatus.Pending)
            .Select(booking => booking.Id)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Добавляет бронь в контекст данных.
    /// </summary>
    public Task AddAsync(Booking booking, CancellationToken ct = default)
    {
        return db.Bookings.AddAsync(booking, ct).AsTask();
    }

    /// <summary>
    /// Сохраняет изменения базы данных броней.
    /// </summary>
    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}
