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
    public async Task<IReadOnlyCollection<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await context.Bookings.AsNoTracking()
            .Where(booking => booking.EventId == eventId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> GetPendingIdsAsync(CancellationToken cancellationToken = default) =>
        await context.Bookings.AsNoTracking()
            .Where(booking => booking.Status == BookingStatus.Pending)
            .Select(booking => booking.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<BookingConfirmationResult> ConfirmAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await context.Bookings.FirstOrDefaultAsync(item => item.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return BookingConfirmationResult.NotFound;
        }

        var eventEntity = await context.Events.FirstOrDefaultAsync(item => item.Id == booking.EventId, cancellationToken);
        if (eventEntity is null)
        {
            booking.Reject(DateTimeOffset.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            return BookingConfirmationResult.EventNotFound;
        }

        booking.Confirm(DateTimeOffset.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
        return BookingConfirmationResult.Confirmed;
    }

    /// <inheritdoc />
    public async Task<bool> TryRejectAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await context.Bookings.FirstOrDefaultAsync(item => item.Id == bookingId, cancellationToken);
        if (booking is null || booking.Status is BookingStatus.Rejected or BookingStatus.Confirmed)
        {
            return true;
        }

        var eventEntity = await context.Events.FirstOrDefaultAsync(item => item.Id == booking.EventId, cancellationToken);
        eventEntity?.ReleaseSeats();
        booking.Reject(DateTimeOffset.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
