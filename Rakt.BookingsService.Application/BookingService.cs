using Rakt.BookingsService.Domain;
using Rakt.BookingsService.Domain.Exceptions;
using Rakt.Contracts.Messaging;
namespace Rakt.BookingsService.Application;
/// <summary>Сценарии создания и отмены броней.</summary>
public sealed class BookingService(IBookingRepository bookings, IBookingMessagePublisher publisher)
{
    /// <summary>
    /// Возвращает бронь по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор брони.</param>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task<Booking> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await bookings.GetAsync(id, ct)
            ?? throw new NotFoundException($"Бронь с идентификатором '{id}' не найдена.");
    }

    /// <summary>
    /// Возвращает брони указанного события.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    public Task<IReadOnlyCollection<Booking>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken ct = default)
    {
        return bookings.GetByEventIdAsync(eventId, ct);
    }

    /// <summary>Создаёт ожидающую бронь и публикует запрос места.</summary>
    public async Task<Booking> CreateAsync(Guid userId, Guid eventId, CancellationToken ct = default)
    {
        var booking = Booking.Create(userId, eventId);

        await bookings.AddAsync(booking, ct);
        await bookings.SaveChangesAsync(ct);
        await publisher.PublishAsync(
            new BookingRequested(booking.Id, userId, eventId, DateTimeOffset.UtcNow),
            ct);

        return booking;
    }
    /// <summary>Отменяет бронь и публикует запрос возврата места.</summary>
    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        await CancelAsync(id, userId: null, isAdministrator: false, ct);
    }

    /// <summary>
    /// Отменяет бронь от имени пользователя или администратора.
    /// </summary>
    /// <param name="id">Идентификатор отменяемой брони.</param>
    /// <param name="userId">Идентификатор пользователя из JWT-токена.</param>
    /// <param name="isAdministrator">Признак роли администратора.</param>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task CancelAsync(
        Guid id,
        Guid? userId,
        bool isAdministrator,
        CancellationToken ct = default)
    {
        var booking = await bookings.GetAsync(id, ct)
            ?? throw new NotFoundException($"Бронь с идентификатором '{id}' не найдена.");

        if (userId.HasValue && booking.UserId != userId && !isAdministrator)
        {
            throw new OperationForbiddenException("Недостаточно прав для отмены этого бронирования.");
        }

        booking.Cancel();

        await bookings.SaveChangesAsync(ct);
        await publisher.PublishAsync(
            new BookingCancelled(booking.Id, booking.EventId, DateTimeOffset.UtcNow),
            ct);
    }
}
