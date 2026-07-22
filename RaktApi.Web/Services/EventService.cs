using RaktApi.Domain;
using RaktApi.Domain.Exceptions;
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

        var events = await _eventRepository.GetAllAsync(query, cancellationToken);

        return new PaginatedResult<EventInfoDto>
        {
            TotalCount = events.TotalCount,
            Items = events.Items.Select(eventEntity => eventEntity.ToInfoDto()).ToList(),
            Page = events.Page,
            PageSize = events.PageSize,
            CurrentCount = events.CurrentCount
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

}
