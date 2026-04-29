using RaktWebApi.Models;

namespace RaktWebApi.Data.Repositories;

/// <summary>
/// Хранение событий в памяти. Реализация репозитория для работы с событиями, которая использует список для хранения данных.
/// </summary>
public class InMemoryEventRepository : IEventRepository
{
    private readonly List<Event> events = [];
    private readonly Lock _lock = new();

    /// <inheritdoc />
    public IEnumerable<Event> GetAll()
    {
        using (_lock.EnterScope())
        {
            return [.. events];
        }
    }

    /// <inheritdoc />
    public Event? GetById(Guid id)
    {
        using (_lock.EnterScope())
        {
            return events.FirstOrDefault(e => e.Id == id);
        }
    }

    /// <inheritdoc />
    public void Add(Event entity)
    {
        using (_lock.EnterScope())
        {
            events.Add(entity);
        }
    }

    /// <inheritdoc />
    public void Delete(Event entity)
    {
        using (_lock.EnterScope())
        {
            events.Remove(entity);
        }
    }
}