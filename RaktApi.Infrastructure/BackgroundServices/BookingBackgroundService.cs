using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaktApi.Application.Services;
using RaktApi.Infrastructure.Options;

namespace RaktApi.Infrastructure.BackgroundServices;

/// <summary>
/// Адаптер hosting-среды, который периодически запускает use case обработки бронирований.
/// </summary>
public sealed class BookingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<BookingProcessingOptions> bookingProcessingOptions,
    ILogger<BookingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private readonly int attemptsLimit = bookingProcessingOptions.Value.AttemptsLimit;

    /// <summary>
    /// Запускает обработку ожидающих бронирований с заданной периодичностью.
    /// </summary>
    /// <param name="stoppingToken">Токен остановки hosted service.</param>
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
    /// Создаёт scope и запускает application use case обработки бронирований.
    /// </summary>
    private async Task ProcessPendingBookingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var bookingProcessingService = scope.ServiceProvider.GetRequiredService<IBookingProcessingService>();
            await bookingProcessingService.ProcessPendingAsync(attemptsLimit, cancellationToken);
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
