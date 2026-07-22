using Microsoft.Extensions.DependencyInjection;
using RaktApi.Domain;
using RaktWebApi.Repositories;

namespace RaktWebApi.Services;

/// <summary>
/// Обработчик бронирований.
/// </summary>
public sealed class BookingProcessor : IBookingProcessor
{
    private static readonly TimeSpan ExternalSystemDelay = TimeSpan.FromSeconds(2);
    private static readonly SemaphoreSlim ProcessingSemaphore = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessor> _logger;

    /// <summary>
    /// Создает обработчик бронирований.
    /// </summary>
    /// <param name="scopeFactory">Фабрика scopes.</param>
    /// <param name="logger">Логгер обработчика.</param>
    public BookingProcessor(IServiceScopeFactory scopeFactory, ILogger<BookingProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Обрабатывает бронирование в фоне.
    /// </summary>
    public async Task ProcessAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Начата обработка брони {BookingId}", booking.Id);
        await Task.Delay(ExternalSystemDelay, cancellationToken);

        await ProcessingSemaphore.WaitAsync(cancellationToken);
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var result = await bookingRepository.ConfirmAsync(booking.Id, cancellationToken);
            if (result == BookingConfirmationResult.NotFound)
            {
                return;
            }

            if (result == BookingConfirmationResult.EventNotFound)
            {
                _logger.LogWarning(
                    "Бронь {BookingId} отклонена, потому что событие {EventId} удалено",
                    booking.Id,
                    booking.EventId);
                return;
            }

            _logger.LogInformation(
                "Бронь {BookingId} переведена в статус {Status}",
                booking.Id,
                BookingStatus.Confirmed);
        }
        finally
        {
            ProcessingSemaphore.Release();
        }
    }

    /// <summary>
    /// Отклоняет бронирование и сохраняет его состояние.
    /// </summary>
    public async Task<bool> TryRejectAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            ArgumentNullException.ThrowIfNull(booking);
            _logger.LogWarning("Отклонение брони {BookingId}", booking.Id);

            await ProcessingSemaphore.WaitAsync(cancellationToken);
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                if (!await bookingRepository.TryRejectAsync(booking.Id, cancellationToken))
                {
                    return false;
                }
            }
            finally
            {
                ProcessingSemaphore.Release();
            }

            _logger.LogWarning(
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
            _logger.LogError(ex, "Не удалось отклонить бронь {BookingId}", booking.Id);
            return false;
        }
    }
}
