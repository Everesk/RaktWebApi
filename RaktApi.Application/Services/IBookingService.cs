using RaktApi.Domain;

namespace RaktApi.Application.Services;

/// <summary>
/// Интерфейс сервиса для работы с бронированиями.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создает бронирование для указанного события.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создает бронирование для указанного события без идентификатора пользователя.
    /// </summary>
    /// <remarks>Временная перегрузка для обратной совместимости. Новые вызовы должны передавать идентификатор пользователя.</remarks>
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает бронирование по идентификатору.
    /// </summary>
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает все бронирования для указанного события.
    /// </summary>
    Task<IReadOnlyCollection<Booking>> GetBookingsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
}
