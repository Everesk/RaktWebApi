using RaktApi.Application.Ports;
using RaktApi.Domain;

namespace RaktApi.Application.Services;

/// <summary>
/// Обработчик подтверждения и отклонения бронирований.
/// </summary>
public sealed class BookingProcessor(
    IBookingRepository bookingRepository,
    IEventRepository eventRepository) : IBookingProcessor
{
    // Синхронизирует обработку бронирований внутри экземпляра приложения.
    private static readonly SemaphoreSlim ProcessingSemaphore = new(1, 1);

    /// <inheritdoc />
    public async Task ProcessAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        cancellationToken.ThrowIfCancellationRequested();

        await ProcessingSemaphore.WaitAsync(cancellationToken);
        try
        {
            var trackedBooking = await bookingRepository.GetForUpdateAsync(booking.Id, cancellationToken);
            if (trackedBooking is null)
            {
                return;
            }

            var eventEntity = await eventRepository.GetForUpdateAsync(trackedBooking.EventId, cancellationToken);
            if (eventEntity is null)
            {
                trackedBooking.Reject(DateTimeOffset.UtcNow);
            }
            else
            {
                trackedBooking.Confirm(DateTimeOffset.UtcNow);
            }

            await bookingRepository.UpdateAsync(cancellationToken);
        }
        finally
        {
            ProcessingSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryRejectAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        cancellationToken.ThrowIfCancellationRequested();

        await ProcessingSemaphore.WaitAsync(cancellationToken);
        try
        {
            var trackedBooking = await bookingRepository.GetForUpdateAsync(booking.Id, cancellationToken);
            if (trackedBooking is null || trackedBooking.Status is BookingStatus.Rejected or BookingStatus.Confirmed)
            {
                return true;
            }

            var eventEntity = await eventRepository.GetForUpdateAsync(trackedBooking.EventId, cancellationToken);
            eventEntity?.ReleaseSeats();
            trackedBooking.Reject(DateTimeOffset.UtcNow);
            await bookingRepository.UpdateAsync(cancellationToken);
            return true;
        }
        finally
        {
            ProcessingSemaphore.Release();
        }
    }
}
