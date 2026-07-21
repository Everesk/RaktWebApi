using RaktWebApi.Common.Exceptions;
using RaktWebApi.Mappers;
using RaktWebApi.Models;
using RaktWebApi.Models.DTO;
using RaktWebApi.Repositories;

namespace RaktWebApi.Services;

/// <summary>
/// Сервис для управления событиями.
/// </summary>
public sealed class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;

    /// <summary>
    /// Создает сервис событий.
    /// </summary>
    /// <param name="eventRepository">Репозиторий событий.</param>
    public EventService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    /// <summary>
    /// Возвращает список событий с учетом фильтров и пагинации.
    /// </summary>
    public async Task<PaginatedResult<EventInfoDto>> GetAllAsync(EventQueryDto query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var events = await _eventRepository.GetAllAsync(cancellationToken);
        IEnumerable<Event> filteredEvents = events;

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            filteredEvents = filteredEvents.Where(e =>
                e.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));
        }

        if (query.From.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.EndAt <= query.To.Value);
        }

        filteredEvents = filteredEvents
            .OrderBy(e => e.StartAt)
            .ThenBy(e => e.Title)
            .ThenBy(e => e.Id);

        var totalCount = filteredEvents.Count();
        var items = ApplyPaging(filteredEvents, query);

        return new PaginatedResult<EventInfoDto>
        {
            TotalCount = totalCount,
            Items = items.Select(e => e.ToInfoDto()).ToList(),
            Page = query.Page ?? 1,
            PageSize = query.PageSize ?? items.Count,
            CurrentCount = items.Count
        };
    }

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    public async Task<EventInfoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingEvent = await _eventRepository.GetByIdAsync(id, cancellationToken);

        return (existingEvent ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.")).ToInfoDto();
    }

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    public async Task<EventInfoDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = dto.CreateFromDto();
        await _eventRepository.AddAsync(entity, cancellationToken);
        return entity.ToInfoDto();
    }

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    public async Task UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingEvent = await _eventRepository.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");

        existingEvent.UpdateFromDto(dto);
        await _eventRepository.UpdateAsync(cancellationToken);
    }

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingEvent = await _eventRepository.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");

        await _eventRepository.DeleteAsync(existingEvent, cancellationToken);
    }

    /// <summary>
    /// Применяет постраничную выборку к уже отсортированной последовательности.
    /// </summary>
    private static List<Event> ApplyPaging(IEnumerable<Event> events, EventQueryDto query)
    {
        if (query.Page.HasValue && query.PageSize.HasValue)
        {
            var page = query.Page.Value;
            var pageSize = query.PageSize.Value;

            return events
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        return events.ToList();
    }
}
