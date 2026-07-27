namespace Rakt.BookingsService.Domain.Exceptions;

/// <summary>Возникает при попытке забронировать уже начавшееся событие.</summary>
public sealed class PastEventBookingException : Exception
{
    /// <summary>Создаёт исключение с описанием нарушения правила.</summary>
    public PastEventBookingException(string message)
        : base(message)
    {
    }
}
