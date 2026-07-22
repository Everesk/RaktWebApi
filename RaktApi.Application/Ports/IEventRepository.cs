using RaktApi.Domain;
using RaktApi.Application.DTO;

namespace RaktApi.Application.Ports;

/// <summary>
/// Предоставляет доступ к данным событий.
/// </summary>
public interface IEventRepository
{
    /// <summary>Возвращает отфильтрованную и постраничную выборку событий без отслеживания изменений.</summary>
    Task<PaginatedResult<Event>> GetAllAsync(EventQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>Возвращает событие по идентификатору без отслеживания изменений.</summary>
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Возвращает событие для изменения.</summary>
    Task<Event?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Добавляет событие и сохраняет изменения.</summary>
    Task AddAsync(Event eventEntity, CancellationToken cancellationToken = default);

    /// <summary>Сохраняет изменения события.</summary>
    Task UpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>Удаляет событие и сохраняет изменения.</summary>
    Task DeleteAsync(Event eventEntity, CancellationToken cancellationToken = default);
}
