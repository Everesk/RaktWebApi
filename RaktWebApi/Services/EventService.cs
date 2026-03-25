using RaktWebApi.Mappers;
using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Сервис для хранения событий в памяти.
/// </summary>
public class EventService : IEventService
{
    private readonly List<Event> events = [];
    
    /// <summary>
    /// Возвращает все события.
    /// </summary>
    public IEnumerable<Event> GetAll() => events;

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    public Event? GetById(Guid id) => events.FirstOrDefault(e => e.Id == id);
    
    /// <summary>
    /// Создает новое событие.
    /// </summary>
    public Event Create(CreateEventDto dto)
    {
        var entity = dto.CreateFromDto();
        events.Add(entity);
        return entity;
    }

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    public bool Update(Guid id, UpdateEventDto dto)
    {
        var existingEvent = GetById(id);

        if (existingEvent is null) return false;
        
        existingEvent.UpdateFromDto(dto);
        return true;
    }

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    public bool Delete(Guid id)
    {
        var existingEvent = GetById(id);

        if (existingEvent is null) return false;

        return events.Remove(existingEvent);
    }
}