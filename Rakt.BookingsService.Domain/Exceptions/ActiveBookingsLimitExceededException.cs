namespace Rakt.BookingsService.Domain.Exceptions;

/// <summary>Возникает при превышении лимита активных бронирований пользователя.</summary>
public sealed class ActiveBookingsLimitExceededException : Exception
{
    /// <summary>Создаёт исключение с описанием нарушения правила.</summary>
    public ActiveBookingsLimitExceededException(string message)
        : base(message)
    {
    }
}
