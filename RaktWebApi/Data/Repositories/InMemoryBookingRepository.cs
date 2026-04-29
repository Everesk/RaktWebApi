using RaktWebApi.Models;

namespace RaktWebApi.Data.Repositories;

/// <summary>
/// Хранение бронирований в памяти.
/// Реализация репозитория для работы с бронированиями, которая использует список для хранения данных.
/// </summary>
public class InMemoryBookingRepository : IBookingRepository
{
    private readonly List<Booking> bookings = [];
    private readonly Lock _lock = new();

    /// <inheritdoc />
    public IEnumerable<Booking> GetAll()
    {
        using (_lock.EnterScope())
        {
            return bookings.ToList();
        }
    }

    /// <inheritdoc />
    public Booking? GetById(Guid id)
    {
        using (_lock.EnterScope())
        {
            return bookings.FirstOrDefault(booking => booking.Id == id);
        }
    }

    /// <inheritdoc />
    public void Add(Booking entity)
    {
        using (_lock.EnterScope())
        {
            bookings.Add(entity);
        }
    }

    /// <inheritdoc />
    public void Delete(Booking entity)
    {
        using (_lock.EnterScope())
        {
            bookings.Remove(entity);
        }
    }
}
