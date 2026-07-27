using Rakt.BookingsService.Application;
using Rakt.BookingsService.Domain;

namespace Rakt.BookingsService.UnitTests;

/// <summary>
/// Простое хранилище броней для изолированных тестов прикладного слоя.
/// </summary>
internal sealed class InMemoryBookingRepository : IBookingRepository
{
    private readonly Dictionary<Guid, Booking> _bookings = [];

    /// <summary>
    /// Количество вызовов сохранения изменений.
    /// </summary>
    public int SaveChangesCount { get; private set; }

    /// <summary>
    /// Возвращает бронь по идентификатору.
    /// </summary>
    public Task<Booking?> GetAsync(Guid id, CancellationToken ct = default)
    {
        _bookings.TryGetValue(id, out var booking);

        return Task.FromResult(booking);
    }

    /// <summary>
    /// Возвращает брони указанного события.
    /// </summary>
    public Task<IReadOnlyCollection<Booking>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken ct = default)
    {
        IReadOnlyCollection<Booking> bookings = _bookings.Values
            .Where(booking => booking.EventId == eventId)
            .ToArray();

        return Task.FromResult(bookings);
    }

    /// <summary>
    /// Добавляет бронь в тестовое хранилище.
    /// </summary>
    public Task AddAsync(Booking booking, CancellationToken ct = default)
    {
        _bookings.Add(booking.Id, booking);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Учитывает операцию сохранения.
    /// </summary>
    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCount++;

        return Task.CompletedTask;
    }
}
