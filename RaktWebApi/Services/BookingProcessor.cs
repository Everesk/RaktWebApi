using RaktWebApi.Data.Repositories;
using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Обработчик бронирований.
/// </summary>
public sealed class BookingProcessor(
    IBookingRepository bookingRepository,
    IEventRepository eventRepository,
    ILogger<BookingProcessor> logger) : IBookingProcessor
{
    private static readonly TimeSpan ExternalSystemDelay = TimeSpan.FromSeconds(2);
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    /// <summary>
    /// Обрабатывает бронирование в фоне.
    /// </summary>
    public async Task ProcessAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Начата обработка брони {BookingId}", booking.Id);

        await Task.Delay(ExternalSystemDelay, cancellationToken);

        Event? eventEntity = null;
        await _processingSemaphore.WaitAsync(cancellationToken);
        try
        {
            eventEntity = eventRepository.GetById(booking.EventId);
            if (eventEntity is null)
            {
                booking.Reject(DateTimeOffset.UtcNow);
                bookingRepository.Update(booking);

                logger.LogWarning(
                    "Бронь {BookingId} отклонена, потому что событие {EventId} удалено",
                    booking.Id,
                    booking.EventId);
                return;
            }

            booking.Confirm(DateTimeOffset.UtcNow);
            bookingRepository.Update(booking);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Непредвиденная ошибка при обработке брони {BookingId}", booking.Id);

            RejectAndReleaseSeats(booking, eventEntity ?? eventRepository.GetById(booking.EventId));
            logger.LogWarning(
                "Бронь {BookingId} отклонена после непредвиденной ошибки",
                booking.Id);
            return;
        }
        finally
        {
            _processingSemaphore.Release();
        }

        logger.LogInformation(
            "Бронь {BookingId} переведена в статус {Status}",
            booking.Id,
            booking.Status);
    }

    /// <summary>
    /// Отклоняет бронирование и сохраняет состояние. Безопасен для вызова, может бросить только OperationCanceledException при отмене через CancellationToken.
    /// </summary>
    public async Task<bool> TryRejectAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            ArgumentNullException.ThrowIfNull(booking);
            logger.LogWarning("Отклонение брони {BookingId}", booking.Id);

            await _processingSemaphore.WaitAsync(cancellationToken);
            try
            {
                RejectAndReleaseSeats(booking, eventRepository.GetById(booking.EventId));
            }
            finally
            {
                _processingSemaphore.Release();
            }

            logger.LogWarning(
                "Бронь {BookingId} переведена в статус {Status}",
                booking.Id,
                booking.Status);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось отклонить бронь {BookingId}", booking.Id);
            return false;
        }
    }

    private void RejectAndReleaseSeats(Booking booking, Event? eventEntity)
    {
        if (booking.Status == BookingStatus.Rejected)
        {
            return;
        }

        if (booking.Status == BookingStatus.Confirmed)
        {
            return;
        }

        if (eventEntity is not null)
        {
            eventEntity.ReleaseSeats();
            eventRepository.Update(eventEntity);
        }

        booking.Reject(DateTimeOffset.UtcNow);
        bookingRepository.Update(booking);
    }
}
