using Microsoft.Extensions.Logging;
using Rakt.BookingsService.Domain;
using Rakt.Contracts.Messaging;

namespace Rakt.BookingsService.Application;

/// <summary>
/// Присваивает брони итоговый статус по ответу сервиса событий.
/// </summary>
public sealed class BookingReservationResultService(
    IBookingRepository bookings,
    ILogger<BookingReservationResultService> logger)
{
    /// <summary>
    /// Подтверждает ожидающую бронь после успешного резервирования места.
    /// </summary>
    /// <param name="message">Результат успешного резервирования.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task HandleAsync(SeatsReserved message, CancellationToken cancellationToken = default)
    {
        var booking = await bookings.GetAsync(message.BookingId, cancellationToken);
        if (booking is null)
        {
            logger.LogWarning("Не найдена бронь {BookingId} для подтверждения", message.BookingId);

            return;
        }

        if (booking.Status != BookingStatus.Pending)
        {
            logger.LogInformation(
                "Игнорируется подтверждение брони {BookingId} в статусе {BookingStatus}",
                booking.Id,
                booking.Status);

            return;
        }

        booking.Confirm(message.OccurredAt);
        await bookings.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Отклоняет ожидающую бронь после отказа сервиса событий.
    /// </summary>
    /// <param name="message">Результат отказа в резервировании.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task HandleAsync(SeatsReservationRejected message, CancellationToken cancellationToken = default)
    {
        var booking = await bookings.GetAsync(message.BookingId, cancellationToken);
        if (booking is null)
        {
            logger.LogWarning("Не найдена бронь {BookingId} для отклонения", message.BookingId);

            return;
        }

        if (booking.Status != BookingStatus.Pending)
        {
            logger.LogInformation(
                "Игнорируется отказ по брони {BookingId} в статусе {BookingStatus}",
                booking.Id,
                booking.Status);

            return;
        }

        booking.Reject(message.OccurredAt);
        await bookings.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Бронь {BookingId} отклонена: {Reason}", booking.Id, message.Reason);
    }
}
