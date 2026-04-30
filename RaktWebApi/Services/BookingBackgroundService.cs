using RaktWebApi.Data.Repositories;
using RaktWebApi.Options;
using RaktWebApi.Models;
using Microsoft.Extensions.Options;

namespace RaktWebApi.Services;

/// <summary>
/// Фоновый сервис для обработки бронирований в статусе Pending.
/// </summary>
public sealed class BookingBackgroundService(
    IBookingRepository bookingRepository,
    IServiceScopeFactory scopeFactory,
    IOptions<BookingProcessingOptions> bookingProcessingOptions,
    ILogger<BookingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private readonly HashSet<Guid> processingBookings = [];
    private readonly Dictionary<Guid, int> processingAttempts = [];
    private readonly Lock syncRoot = new();
    private readonly int attemptsLimit = bookingProcessingOptions.Value.AttemptsLimit;

    /// <summary>
    /// Основной цикл фоновой обработки.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Фоновая обработка бронирований запущена");

        using var timer = new PeriodicTimer(PollInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProcessPendingBookingsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Фоновая обработка бронирований остановлена");
        }
    }

    /// <summary>
    /// Обрабатывает все бронирования в статусе Pending.
    /// </summary>
    private async Task ProcessPendingBookingsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var pendingBookings = bookingRepository
            .GetAll()
            .Where(booking => booking.Status == BookingStatus.Pending)
            .ToList();

        foreach (var booking in pendingBookings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryMarkProcessing(booking.Id))
            {
                continue;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var bookingProcessor = scope.ServiceProvider.GetRequiredService<IBookingProcessor>();

                await bookingProcessor.ProcessAsync(booking, cancellationToken);
                ClearAttempts(booking.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var attempt = RegisterAttempt(booking.Id);
                logger.LogError(
                    ex,
                    "Не удалось обработать бронь {BookingId} на попытке {Attempt} из {AttemptsLimit}",
                    booking.Id,
                    attempt,
                    attemptsLimit);

                if (attempt < attemptsLimit)
                {
                    continue;
                }

                using var scope = scopeFactory.CreateScope();
                var bookingProcessor = scope.ServiceProvider.GetRequiredService<IBookingProcessor>();

                var rejected = await bookingProcessor.TryRejectAsync(booking, cancellationToken);
                if (rejected)
                {
                    ClearAttempts(booking.Id);
                    logger.LogWarning(
                        "Бронь {BookingId} отклонена после {AttemptsLimit} неудачных попыток",
                        booking.Id,
                        attemptsLimit);
                }
                else
                {
                    logger.LogError(
                        "Не удалось ни обработать ни отклонить бронь {BookingId}",
                        booking.Id); // Тупик, с бронью не удается ничего сделать и она повисла в репозитории навечно в статусе Pending. Тут надо вызывать алярм сисадмину
                }
            }
            finally
            {
                UnmarkProcessing(booking.Id);
            }
        }
    }

    /// <summary>
    /// Помечает бронь как находящуюся в обработке.
    /// </summary>
    private bool TryMarkProcessing(Guid bookingId)
    {
        using (syncRoot.EnterScope())
        {
            return processingBookings.Add(bookingId);
        }
    }

    /// <summary>
    /// Убирает бронь из множества обрабатываемых.
    /// </summary>
    private void UnmarkProcessing(Guid bookingId)
    {
        using (syncRoot.EnterScope())
        {
            processingBookings.Remove(bookingId);
        }
    }

    /// <summary>
    /// Регистрирует очередную попытку обработки бронирования.
    /// </summary>
    private int RegisterAttempt(Guid bookingId)
    {
        using (syncRoot.EnterScope())
        {
            processingAttempts.TryGetValue(bookingId, out var currentAttempt);

            var nextAttempt = currentAttempt + 1;
            processingAttempts[bookingId] = nextAttempt;

            return nextAttempt;
        }
    }

    /// <summary>
    /// Убирает счетчик попыток для бронирования.
    /// </summary>
    private void ClearAttempts(Guid bookingId)
    {
        using (syncRoot.EnterScope())
        {
            processingAttempts.Remove(bookingId);
        }
    }
}
