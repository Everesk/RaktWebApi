using Microsoft.EntityFrameworkCore;
using RaktApi.Application.Ports;
using RaktApi.Domain;
using RaktApi.Infrastructure.Data;

namespace RaktApi.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для доступа к бронированиям через EF Core.
/// </summary>
public sealed class BookingRepository(AppDbContext context) : IBookingRepository
{
    /// <inheritdoc />
    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await context.Bookings.AddAsync(booking, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        context.Bookings.AsNoTracking().FirstOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken);

    /// <inheritdoc />
    public Task<Booking?> GetForUpdateAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        context.Bookings.FirstOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await context.Bookings.AsNoTracking()
            .Where(booking => booking.EventId == eventId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Bookings.CountAsync(
            booking => booking.UserId == userId &&
                       (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed),
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> GetPendingIdsAsync(CancellationToken cancellationToken = default) =>
        await context.Bookings.AsNoTracking()
            .Where(booking => booking.Status == BookingStatus.Pending)
            .Select(booking => booking.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
}
