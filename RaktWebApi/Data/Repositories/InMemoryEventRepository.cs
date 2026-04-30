using System.Collections.Concurrent;
using RaktWebApi.Models;

namespace RaktWebApi.Data.Repositories;

/// <summary>
/// Хранение событий в памяти.
/// Реализация репозитория для работы с событиями, которая использует ConcurrentDictionary.
/// </summary>
public class InMemoryEventRepository : IEventRepository
{
    private readonly ConcurrentDictionary<Guid, Event> events = new();

    /// <inheritdoc />
    public IEnumerable<Event> GetAll()
    {
        return [.. events.Values];
    }

    /// <inheritdoc />
    public Event? GetById(Guid id)
    {
        return events.TryGetValue(id, out var eventEntity) ? eventEntity : null;
    }

    /// <inheritdoc />
    public void Add(Event entity)
    {
        events[entity.Id] = entity;
    }

    /// <inheritdoc />
    public void Delete(Event entity)
    {
        events.TryRemove(entity.Id, out _);
    }
}
