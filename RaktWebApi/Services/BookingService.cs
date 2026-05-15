using System.ComponentModel.DataAnnotations;
using RaktWebApi.Common.Exceptions;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService(
    IBookingRepository bookingRepository,
    IEventRepository eventRepository) : IBookingService
{
    /// <summary>
    /// Создает бронирование для указанного события.
    /// </summary>
    public Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var eventEntity = eventRepository.GetById(eventId);
        if (eventEntity is null)
        {
            throw new NotFoundException($"Событие с идентификатором '{eventId}' не найдено.");
        }

        if (!eventEntity.TryReserveSeats())
        {
            throw new ValidationException($"На событие с идентификатором '{eventId}' нет свободных мест.");
        }

        eventRepository.Update(eventEntity);

        var booking = new Booking(eventId);
        bookingRepository.Add(booking);

        return Task.FromResult(booking);
    }

    /// <summary>
    /// Возвращает бронирование по идентификатору.
    /// </summary>
    public Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var booking = bookingRepository.GetById(bookingId);

        return Task.FromResult(
            booking ?? throw new NotFoundException($"Бронь с идентификатором '{bookingId}' не найдена."));
    }
}
