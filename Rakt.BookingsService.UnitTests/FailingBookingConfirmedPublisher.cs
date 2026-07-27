using Rakt.BookingsService.Application;
using Rakt.Contracts.Messaging;

namespace Rakt.BookingsService.UnitTests;

/// <summary>
/// Имитирует недоступность брокера при публикации подтверждения.
/// </summary>
internal sealed class FailingBookingConfirmedPublisher : IBookingConfirmedPublisher
{
    /// <summary>
    /// Число попыток публикации.
    /// </summary>
    public int PublishAttemptsCount { get; private set; }

    /// <summary>
    /// Завершает публикацию ошибкой.
    /// </summary>
    public Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken = default)
    {
        PublishAttemptsCount++;

        throw new InvalidOperationException("Kafka недоступна.");
    }
}
