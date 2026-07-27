using Rakt.BookingsService.Application;
using Rakt.Contracts.Messaging;

namespace Rakt.BookingsService.UnitTests;

/// <summary>
/// Запоминает подтверждённое событие и момент его публикации.
/// </summary>
internal sealed class RecordingBookingConfirmedPublisher : IBookingConfirmedPublisher
{
    private readonly Func<int> _getSaveChangesCount;

    /// <summary>
    /// Создаёт издатель с доступом к числу сохранений хранилища.
    /// </summary>
    public RecordingBookingConfirmedPublisher(Func<int> getSaveChangesCount)
    {
        _getSaveChangesCount = getSaveChangesCount;
    }

    /// <summary>
    /// Последнее опубликованное событие.
    /// </summary>
    public BookingConfirmed? PublishedMessage { get; private set; }

    /// <summary>
    /// Число сохранений на момент публикации.
    /// </summary>
    public int SaveChangesCountAtPublication { get; private set; }

    /// <summary>
    /// Запоминает подтверждённое событие.
    /// </summary>
    public Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken = default)
    {
        SaveChangesCountAtPublication = _getSaveChangesCount();
        PublishedMessage = message;

        return Task.CompletedTask;
    }
}
