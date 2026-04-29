using RaktWebApi.Data.Repositories;
using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Фоновый сервис для обработки бронирований в статусе Pending.
/// </summary>
public sealed class BookingBackgroundService(
    IBookingRepository bookingRepository,
    IBookingProcessor bookingProcessor,
    ILogger<BookingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private readonly HashSet<Guid> processingBookings = [];
    private readonly object syncRoot = new();

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
        var pendingBookings = bookingRepository
            .GetAll()
            .Where(booking => booking.Status == BookingStatus.Pending)
            .ToList();

        foreach (var booking in pendingBookings)
        {
            if (!TryMarkProcessing(booking.Id))
            {
                continue;
            }

            try
            {
                await bookingProcessor.ProcessAsync(booking, cancellationToken);
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
}
