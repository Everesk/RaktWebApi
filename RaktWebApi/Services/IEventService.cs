using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Интерфейс сервиса для управления событиями.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Возвращает список событий с учетом фильтрации и пагинации.
    /// </summary>
    PaginatedResult<Event> GetAll(EventQueryDto query);

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    Event GetById(Guid id);

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    Event Create(CreateEventDto dto);

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    void Update(Guid id, UpdateEventDto dto);

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    void Delete(Guid id);
}