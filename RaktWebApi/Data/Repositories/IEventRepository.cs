using RaktWebApi.Models;

namespace RaktWebApi.Data.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с событиями.
/// Отвечает за доступ к данным и их хранение.
/// </summary>
public interface IEventRepository
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
    /// Добавляет новое событие.
    /// </summary>
    void Add(Event entity);

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    void Update(Event entity);

    /// <summary>
    /// Удаляет событие.
    /// </summary>
    void Delete(Event entity);
}
