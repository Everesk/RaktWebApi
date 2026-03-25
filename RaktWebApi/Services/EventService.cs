using RaktWebApi.Mappers;
using RaktWebApi.Models;
using System.Threading;

namespace RaktWebApi.Services;

/// <summary>
/// Сервис для хранения событий в памяти.
/// </summary>
public class EventService : IEventService
{
    private readonly List<Event> events = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// Возвращает все события.
    /// </summary>
    public IEnumerable<Event> GetAll()
    {
        // throw new Exception("Проверка глобального обработчика");

        using (_lock.EnterScope()) return events.ToList();
    }

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    public Event? GetById(Guid id)
    {
        using (_lock.EnterScope()) return events.FirstOrDefault(e => e.Id == id);
    }

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    public Event Create(CreateEventDto dto)
    {
        var entity = dto.CreateFromDto();

        using (_lock.EnterScope())
        {
            events.Add(entity);
        }

        return entity;
    }

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    public bool Update(Guid id, UpdateEventDto dto)
    {
        using (_lock.EnterScope())
        {
            var existingEvent = events.FirstOrDefault(e => e.Id == id);

            if (existingEvent is null) return false;

            existingEvent.UpdateFromDto(dto);
            return true;
        }
    }

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    public bool Delete(Guid id)
    {
        using (_lock.EnterScope())
        {
            var existingEvent = events.FirstOrDefault(e => e.Id == id);

            if (existingEvent is null) return false;

            return events.Remove(existingEvent);
        }
    }
}