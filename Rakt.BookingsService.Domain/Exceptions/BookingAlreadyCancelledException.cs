namespace Rakt.BookingsService.Domain.Exceptions;

/// <summary>Возникает при повторной отмене брони.</summary>
public sealed class BookingAlreadyCancelledException : Exception
{
    /// <summary>Создаёт исключение с описанием нарушения правила.</summary>
    public BookingAlreadyCancelledException(string message)
        : base(message)
    {
    }
}
