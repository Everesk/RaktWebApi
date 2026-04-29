using RaktWebApi.Common.Exceptions;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService(IBookingRepository repository) : IBookingService
{
    /// <summary>
    /// Создает бронирование для указанного события.
    /// </summary>
    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        var booking = new Booking(eventId);
        repository.Add(booking);

        return Task.FromResult(booking);
    }

    /// <summary>
    /// Возвращает бронирование по идентификатору.
    /// </summary>
    public Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = repository.GetById(bookingId);

        return Task.FromResult(
            booking ?? throw new NotFoundException($"Бронь с идентификатором '{bookingId}' не найдена."));
    }
}
