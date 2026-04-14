using RaktWebApi.Common.Exceptions;
using RaktWebApi.Mappers;
using RaktWebApi.Models;

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
        using (_lock.EnterScope())
        {
            return events.ToList();
        }
    }

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    public Event GetById(Guid id)
    {
        using (_lock.EnterScope())
        {
            var existingEvent = events.FirstOrDefault(e => e.Id == id);

            if (existingEvent is null)
            {
                throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
            }

            return existingEvent;
        }
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
    public void Update(Guid id, UpdateEventDto dto)
    {
        using (_lock.EnterScope())
        {
            var existingEvent = events.FirstOrDefault(e => e.Id == id);

            if (existingEvent is null)
            {
                throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
            }

            existingEvent.UpdateFromDto(dto);
        }
    }

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    public void Delete(Guid id)
    {
        using (_lock.EnterScope())
        {
            var existingEvent = events.FirstOrDefault(e => e.Id == id);

            if (existingEvent is null)
            {
                throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
            }

            events.Remove(existingEvent);
        }
    }
}