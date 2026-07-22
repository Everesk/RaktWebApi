using RaktApi.Application.Ports;
using RaktApi.Domain;

namespace RaktApi.Application.Services;

/// <summary>
/// Обработчик подтверждения и отклонения бронирований.
/// </summary>
public sealed class BookingProcessor(IBookingRepository bookingRepository) : IBookingProcessor
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
            await bookingRepository.ConfirmAsync(booking.Id, cancellationToken);
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
            return await bookingRepository.TryRejectAsync(booking.Id, cancellationToken);
        }
        finally
        {
            ProcessingSemaphore.Release();
        }
    }
}
