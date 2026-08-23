namespace Rakt.EventsService.Domain.Exceptions;

/// <summary>Возникает при создании события с недопустимым количеством мест.</summary>
public sealed class InvalidTotalSeatsException : Exception
{
    /// <summary>Создаёт исключение с описанием нарушения правила.</summary>
    public InvalidTotalSeatsException(string message)
        : base(message)
    {
    }
}
