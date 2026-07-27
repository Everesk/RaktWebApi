using Rakt.BookingsService.Application;
using Rakt.Contracts.Messaging;

namespace Rakt.BookingsService.UnitTests;

/// <summary>
/// Запоминает сообщения создания и отмены брони.
/// </summary>
internal sealed class RecordingBookingMessagePublisher : IBookingMessagePublisher
{
    /// <summary>
    /// Последнее сообщение о создании брони.
    /// </summary>
    public BookingRequested? RequestedMessage { get; private set; }

    /// <summary>
    /// Последнее сообщение об отмене брони.
    /// </summary>
    public BookingCancelled? CancelledMessage { get; private set; }

    /// <summary>
    /// Запоминает запрос на создание брони.
    /// </summary>
    public Task PublishAsync(BookingRequested message, CancellationToken ct = default)
    {
        RequestedMessage = message;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Запоминает запрос на отмену брони.
    /// </summary>
    public Task PublishAsync(BookingCancelled message, CancellationToken ct = default)
    {
        CancelledMessage = message;

        return Task.CompletedTask;
    }
}
