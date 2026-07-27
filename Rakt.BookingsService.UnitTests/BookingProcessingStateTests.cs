using Rakt.BookingsService.Application;
using Xunit;

namespace Rakt.BookingsService.UnitTests;

/// <summary>
/// Проверки состояния фоновой обработки броней.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BookingProcessingStateTests
{
    /// <summary>
    /// Счётчик должен хранить число ошибок до явной очистки.
    /// </summary>
    [Fact]
    public void RegisterAttempt_IncrementsAndClearAttemptsResetsCounter()
    {
        var state = new BookingProcessingState();
        var bookingId = Guid.NewGuid();

        var firstAttempt = state.RegisterAttempt(bookingId);
        var secondAttempt = state.RegisterAttempt(bookingId);
        state.ClearAttempts(bookingId);
        var attemptAfterClear = state.RegisterAttempt(bookingId);

        Assert.Equal(1, firstAttempt);
        Assert.Equal(2, secondAttempt);
        Assert.Equal(1, attemptAfterClear);
    }

    /// <summary>
    /// Одна бронь не должна одновременно обрабатываться дважды.
    /// </summary>
    [Fact]
    public void TryMarkProcessing_PreventsConcurrentProcessing()
    {
        var state = new BookingProcessingState();
        var bookingId = Guid.NewGuid();

        var firstMark = state.TryMarkProcessing(bookingId);
        var secondMark = state.TryMarkProcessing(bookingId);
        state.UnmarkProcessing(bookingId);
        var markAfterUnmark = state.TryMarkProcessing(bookingId);

        Assert.True(firstMark);
        Assert.False(secondMark);
        Assert.True(markAfterUnmark);
    }
}
