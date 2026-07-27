using Rakt.BookingsService.Domain;
namespace Rakt.BookingsService.Application;
/// <summary>Порт хранения броней.</summary>
public interface IBookingRepository
{
    /// <summary>
    /// Возвращает отслеживаемую бронь по идентификатору.
    /// </summary>
    Task<Booking?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Возвращает брони указанного события.
    /// </summary>
    Task<IReadOnlyCollection<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Возвращает идентификаторы броней, ожидающих фоновой обработки.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetPendingIdsAsync(CancellationToken ct = default);

    /// <summary>
    /// Добавляет новую бронь в хранилище.
    /// </summary>
    Task AddAsync(Booking booking, CancellationToken ct = default);

    /// <summary>
    /// Сохраняет накопленные изменения.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
