using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rakt.BookingsService.Application;

namespace Rakt.BookingsService.Infrastructure;

/// <summary>
/// С заданной периодичностью подтверждает ожидающие брони.
/// </summary>
public sealed class BookingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<BookingProcessingOptions> processingOptions,
    ILogger<BookingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private readonly int _attemptsLimit = processingOptions.Value.AttemptsLimit;

    /// <summary>
    /// Запускает периодическую обработку ожидающих броней.
    /// </summary>
    /// <param name="stoppingToken">Токен остановки фонового сервиса.</param>
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
    /// Создаёт отдельную область зависимостей и запускает прикладной сценарий.
    /// </summary>
    /// <param name="cancellationToken">Токен остановки фонового сервиса.</param>
    private async Task ProcessPendingBookingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var processingService = scope.ServiceProvider.GetRequiredService<IBookingProcessingService>();

            await processingService.ProcessPendingAsync(_attemptsLimit, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Не удалось обработать ожидающие бронирования");
        }
    }
}
