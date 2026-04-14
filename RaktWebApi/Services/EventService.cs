using RaktWebApi.Common.Exceptions;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Mappers;
using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Сервис для управления событиями.
/// Содержит бизнес-логику и координирует работу с репозиторием.
/// </summary>
public class EventService(IEventRepository repository) : IEventService
{
    /// <summary>
    /// Возвращает все события.
    /// </summary>
    public IEnumerable<Event> GetAll()
    {
        return repository.GetAll();
    }

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    public Event GetById(Guid id)
    {
        var existingEvent = repository.GetById(id);

        if (existingEvent is null)
        {
            throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
        }

        return existingEvent;
    }

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    public Event Create(CreateEventDto dto)
    {
        var entity = dto.CreateFromDto();
        repository.Add(entity);
        return entity;
    }

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    public void Update(Guid id, UpdateEventDto dto)
    {
        var existingEvent = repository.GetById(id);

        if (existingEvent is null)
        {
            throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
        }

        existingEvent.UpdateFromDto(dto);
    }

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    public void Delete(Guid id)
    {
        var existingEvent = repository.GetById(id);

        if (existingEvent is null)
        {
            throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
        }

        repository.Delete(existingEvent);
    }
}