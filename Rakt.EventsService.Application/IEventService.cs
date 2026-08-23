namespace Rakt.EventsService.Application;
/// <summary>Сценарии управления событиями.</summary>
public interface IEventService
{
    /// <summary>Возвращает список событий.</summary>
    Task<PaginatedResult<EventInfoDto>> GetAllAsync(EventQueryDto query, CancellationToken ct = default);
    /// <summary>Возвращает событие.</summary>
    Task<EventInfoDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Создаёт событие.</summary>
    Task<EventInfoDto> CreateAsync(CreateEventDto dto, CancellationToken ct = default);
    /// <summary>Обновляет событие.</summary>
    Task UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken ct = default);
    /// <summary>Удаляет событие.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
