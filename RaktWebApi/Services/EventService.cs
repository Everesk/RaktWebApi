using Microsoft.Extensions.Logging;
using RaktWebApi.Common.Exceptions;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Mappers;
using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Сервис для управления событиями.
/// Содержит бизнес-логику и координирует работу с репозиторием.
/// </summary>
public class EventService : IEventService
{
    private readonly IEventRepository repository;

    public EventService(IEventRepository repository)
    {
        this.repository = repository;
    }

    /// <summary>
    /// Возвращает список событий с учетом фильтров и пагинации.
    /// </summary>
    public PaginatedResult<Event> GetAll(EventQueryDto query)
    {
        var events = repository.GetAll().AsQueryable(); // не обязательно конечно AsQueryable, но пускай будет на будущее

        // Фильтрация
        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            events = events.Where(e =>
                e.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));
        }

        if (query.From.HasValue)
        {
            events = events.Where(e => e.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            events = events.Where(e => e.EndAt <= query.To.Value);
        }

        var totalCount = events.Count();

        // пагинация
        if (query.Page.HasValue && query.PageSize.HasValue)
        {
            var page = query.Page.Value;
            var pageSize = query.PageSize.Value;

            var items = events
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PaginatedResult<Event>
            {
                TotalCount = totalCount,
                Items = items,
                Page = page,
                PageSize = pageSize,
                CurrentCount = items.Count
            };
        }

        // без пагинации (т.е. хотя бы один из параметров не указан)
        var allItems = events.ToList();

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Items = allItems,
            Page = 1,
            PageSize = allItems.Count,
            CurrentCount = allItems.Count
        };
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