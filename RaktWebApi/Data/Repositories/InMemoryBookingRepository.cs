using System.Collections.Concurrent;
using RaktWebApi.Models;

namespace RaktWebApi.Data.Repositories;

/// <summary>
/// Хранение бронирований в памяти.
/// Реализация репозитория для работы с бронированиями, которая использует список для хранения данных.
/// </summary>
public class InMemoryBookingRepository : IBookingRepository
{
    private readonly ConcurrentDictionary<Guid, Booking> bookings = new();

    /// <inheritdoc />
    public IEnumerable<Booking> GetAll()
    {
        return bookings.Values.ToList();
    }

    /// <inheritdoc />
    public Booking? GetById(Guid id)
    {
        return bookings.TryGetValue(id, out var booking) ? booking : null;
    }

    /// <inheritdoc />
    public void Add(Booking entity)
    {
        bookings[entity.Id] = entity;
    }

    /// <inheritdoc />
    public void Update(Booking entity)
    {
        bookings[entity.Id] = entity;
    }

    /// <inheritdoc />
    public void Delete(Booking entity)
    {
        bookings.TryRemove(entity.Id, out _);
    }
}
