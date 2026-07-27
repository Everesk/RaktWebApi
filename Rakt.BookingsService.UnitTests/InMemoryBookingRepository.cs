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
    /// Возвращает идентификаторы броней в статусе ожидания.
    /// </summary>
    public Task<IReadOnlyCollection<Guid>> GetPendingIdsAsync(CancellationToken ct = default)
    {
        IReadOnlyCollection<Guid> pendingBookingIds = _bookings.Values
            .Where(booking => booking.Status == BookingStatus.Pending)
            .Select(booking => booking.Id)
            .ToArray();

        return Task.FromResult(pendingBookingIds);
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
