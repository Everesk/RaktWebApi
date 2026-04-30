using RaktWebApi.Models;

namespace RaktWebApi.Data.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с бронированиями.
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Возвращает все бронирования.
    /// </summary>
    IEnumerable<Booking> GetAll();

    /// <summary>
    /// Возвращает бронирование по идентификатору.
    /// </summary>
    Booking? GetById(Guid id);

    /// <summary>
    /// Добавляет новое бронирование.
    /// </summary>
    void Add(Booking entity);

    /// <summary>
    /// Обновляет бронирование.
    /// </summary>
    void Update(Booking entity);

    /// <summary>
    /// Удаляет бронирование.
    /// </summary>
    void Delete(Booking entity);
}
