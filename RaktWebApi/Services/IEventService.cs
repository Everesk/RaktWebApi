using RaktWebApi.Models;
using RaktWebApi.Models.DTO;

namespace RaktWebApi.Services;

/// <summary>
/// Интерфейс сервиса для управления событиями.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Возвращает список событий с учетом фильтрации и пагинации.
    /// </summary>
    Task<PaginatedResult<Event>> GetAllAsync(EventQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    Task<EventInfoDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    Task UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
