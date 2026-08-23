namespace Rakt.EventsService.Domain.Exceptions;

/// <summary>Возникает, когда для события не осталось свободных мест.</summary>
public sealed class NoAvailableSeatsException : Exception
{
    /// <summary>Создаёт исключение с описанием нарушения правила.</summary>
    public NoAvailableSeatsException(string message)
        : base(message)
    {
    }
}
