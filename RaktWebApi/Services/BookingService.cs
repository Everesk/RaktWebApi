using Microsoft.EntityFrameworkCore;
using RaktWebApi.Common.Exceptions;
using RaktWebApi.Data;
using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public sealed class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);
    private readonly AppDbContext _context;

    /// <summary>
    /// Создает сервис бронирований.
    /// </summary>
    /// <param name="context">Контекст базы данных приложения.</param>
    public BookingService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Создает бронирование для указанного события.
    /// </summary>
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await BookingSemaphore.WaitAsync(cancellationToken);
        try
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
            if (eventEntity is null)
            {
                throw new NotFoundException($"Событие с идентификатором '{eventId}' не найдено.");
            }

            if (!eventEntity.TryReserveSeats())
            {
                throw new NoAvailableSeatsException("Мест нет, уйдите");
            }

            var booking = new Booking(eventId);
            await _context.Bookings.AddAsync(booking, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return booking;
        }
        finally
        {
            BookingSemaphore.Release();
        }
    }

    /// <summary>
    /// Возвращает бронирование по идентификатору.
    /// </summary>
    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var booking = await _context.Bookings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken);

        return booking ?? throw new NotFoundException($"Бронь с идентификатором '{bookingId}' не найдена.");
    }

    /// <summary>
    /// Возвращает все бронирования для указанного события.
    /// </summary>
    public async Task<IReadOnlyCollection<Booking>> GetBookingsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var eventExists = await _context.Events.AsNoTracking()
            .AnyAsync(x => x.Id == eventId, cancellationToken);

        if (!eventExists)
        {
            throw new NotFoundException($"Событие с идентификатором '{eventId}' не найдено.");
        }

        var bookings = await _context.Bookings.AsNoTracking()
            .Where(booking => booking.EventId == eventId)
            .ToListAsync(cancellationToken);

        return bookings;
    }
}
