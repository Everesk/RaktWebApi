using Microsoft.EntityFrameworkCore;
using RaktWebApi.Common.Exceptions;
using RaktWebApi.Data;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Mappers;
using RaktWebApi.Models;
using RaktWebApi.Models.DTO;

namespace RaktWebApi.Services;

/// <summary>
/// Сервис для управления событиями.
/// </summary>
public class EventService : IEventService
{
    private readonly AppDbContext? _context;
    private readonly IEventRepository? _repository;

    /// <summary>
    /// Создает сервис событий для работы через EF Core.
    /// </summary>
    /// <param name="context">Контекст базы данных приложения.</param>
    public EventService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Создает сервис событий для работы через репозиторий.
    /// </summary>
    /// <param name="repository">Репозиторий событий.</param>
    public EventService(IEventRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Возвращает список событий с учетом фильтров и пагинации.
    /// </summary>
    public async Task<PaginatedResult<Event>> GetAllAsync(EventQueryDto query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_context is not null)
        {
            var events = await _context.Events.AsNoTracking().ToListAsync(cancellationToken);
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
            var items = ApplyPaging(filteredEvents, query, totalCount);

            return new PaginatedResult<Event>
            {
                TotalCount = totalCount,
                Items = items,
                Page = query.Page ?? 1,
                PageSize = query.PageSize ?? items.Count,
                CurrentCount = items.Count
            };
        }

        var repository = GetRepository();
        var repositoryEvents = repository.GetAll().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            repositoryEvents = repositoryEvents.Where(e =>
                e.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));
        }

        if (query.From.HasValue)
        {
            repositoryEvents = repositoryEvents.Where(e => e.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            repositoryEvents = repositoryEvents.Where(e => e.EndAt <= query.To.Value);
        }

        repositoryEvents = repositoryEvents
            .OrderBy(e => e.StartAt)
            .ThenBy(e => e.Title)
            .ThenBy(e => e.Id);

        var repositoryTotalCount = repositoryEvents.Count();
        var repositoryItems = ApplyPaging(repositoryEvents, query, repositoryTotalCount);

        return await Task.FromResult(new PaginatedResult<Event>
        {
            TotalCount = repositoryTotalCount,
            Items = repositoryItems,
            Page = query.Page ?? 1,
            PageSize = query.PageSize ?? repositoryItems.Count,
            CurrentCount = repositoryItems.Count
        });
    }

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    public async Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_context is not null)
        {
            var existingEvent = await _context.Events.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            return existingEvent ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
        }

        var repository = GetRepository();
        var repositoryEvent = repository.GetById(id);

        return await Task.FromResult(
            repositoryEvent ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено."));
    }

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    public async Task<EventInfoDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = dto.CreateFromDto();

        if (_context is not null)
        {
            await _context.Events.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return entity.ToInfoDto();
        }

        GetRepository().Add(entity);
        return await Task.FromResult(entity.ToInfoDto());
    }

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    public async Task UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_context is not null)
        {
            var existingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
                ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");

            existingEvent.UpdateFromDto(dto);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var repository = GetRepository();
        var repositoryEvent = repository.GetById(id)
            ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");

        repositoryEvent.UpdateFromDto(dto);
        repository.Update(repositoryEvent);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_context is not null)
        {
            var existingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
                ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");

            _context.Events.Remove(existingEvent);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var repository = GetRepository();
        var repositoryEvent = repository.GetById(id)
            ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");

        repository.Delete(repositoryEvent);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Применяет постраничную выборку к уже отсортированной последовательности.
    /// </summary>
    private static List<Event> ApplyPaging(IEnumerable<Event> events, EventQueryDto query, int totalCount)
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

    /// <summary>
    /// Возвращает репозиторий событий для обратной совместимости с тестами.
    /// </summary>
    private IEventRepository GetRepository()
    {
        return _repository ?? throw new InvalidOperationException("Репозиторий событий не настроен.");
    }
}
