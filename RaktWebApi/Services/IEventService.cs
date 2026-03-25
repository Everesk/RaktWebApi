using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Интерфейсм сервиса для работы с событиями.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Возвращает все события.
    /// </summary>
    IEnumerable<Event> GetAll();

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    Event? GetById(Guid id);

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    Event Create(CreateEventDto dto);

    /// <summary>
    /// Обновляет существующее событие (полностью).
    /// </summary>
    bool Update(Guid id, UpdateEventDto dto);

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    bool Delete(Guid id);
}