using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaktWebApi.Data;
using RaktWebApi.Models;
using RaktWebApi.Options;

namespace RaktWebApi.Services;

/// <summary>
/// Фоновый сервис для обработки бронирований в статусе Pending.
/// </summary>
public sealed class BookingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<BookingProcessingOptions> bookingProcessingOptions,
    ILogger<BookingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private readonly HashSet<Guid> processingBookings = [];
    private readonly Dictionary<Guid, int> processingAttempts = [];
    private readonly object syncRoot = new();
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

        List<Guid> pendingBookingIds;
        using (var scope = scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            pendingBookingIds = await context.Bookings
                .AsNoTracking()
                .Where(booking => booking.Status == BookingStatus.Pending)
                .Select(booking => booking.Id)
                .ToListAsync(cancellationToken);
        }

        var tasks = pendingBookingIds.Select(bookingId => ProcessBookingAsync(bookingId, cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Обрабатывает одну бронь.
    /// </summary>
    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryMarkProcessing(bookingId))
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var booking = await context.Bookings
                .FirstOrDefaultAsync(item => item.Id == bookingId, cancellationToken);

            if (booking is null || booking.Status != BookingStatus.Pending)
            {
                ClearAttempts(bookingId);
                return;
            }

            var bookingProcessor = scope.ServiceProvider.GetRequiredService<IBookingProcessor>();
            await bookingProcessor.ProcessAsync(booking, cancellationToken);
            ClearAttempts(bookingId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var attempt = RegisterAttempt(bookingId);
            logger.LogError(
                ex,
                "Не удалось обработать бронь {BookingId} на попытке {Attempt} из {AttemptsLimit}",
                bookingId,
                attempt,
                attemptsLimit);

            if (attempt < attemptsLimit)
            {
                return;
            }

            var rejected = await TryRejectBookingAsync(bookingId, cancellationToken);
            if (rejected)
            {
                ClearAttempts(bookingId);
                logger.LogWarning(
                    "Бронь {BookingId} отклонена после {AttemptsLimit} неудачных попыток",
                    bookingId,
                    attemptsLimit);
            }
            else
            {
                logger.LogError(
                    "Не удалось ни обработать ни отклонить бронь {BookingId}",
                    bookingId);
            }
        }
        finally
        {
            UnmarkProcessing(bookingId);
        }
    }

    /// <summary>
    /// Пытается отклонить бронь в отдельном scope.
    /// </summary>
    private async Task<bool> TryRejectBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await context.Bookings
            .FirstOrDefaultAsync(item => item.Id == bookingId, cancellationToken);

        if (booking is null)
        {
            return false;
        }

        var bookingProcessor = scope.ServiceProvider.GetRequiredService<IBookingProcessor>();
        return await bookingProcessor.TryRejectAsync(booking, cancellationToken);
    }

    /// <summary>
    /// Помечает бронь как находящуюся в обработке.
    /// </summary>
    private bool TryMarkProcessing(Guid bookingId)
    {
        lock (syncRoot)
        {
            return processingBookings.Add(bookingId);
        }
    }

    /// <summary>
    /// Убирает бронь из множества обрабатываемых.
    /// </summary>
    private void UnmarkProcessing(Guid bookingId)
    {
        lock (syncRoot)
        {
            processingBookings.Remove(bookingId);
        }
    }

    /// <summary>
    /// Регистрирует очередную попытку обработки бронирования.
    /// </summary>
    private int RegisterAttempt(Guid bookingId)
    {
        lock (syncRoot)
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
        lock (syncRoot)
        {
            processingAttempts.Remove(bookingId);
        }
    }
}
