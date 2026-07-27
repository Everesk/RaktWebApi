namespace Rakt.BookingsService.Domain.Exceptions;

/// <summary>Возникает при отсутствии прав на выполнение операции с бронью.</summary>
public sealed class OperationForbiddenException : Exception
{
    /// <summary>Создаёт исключение с описанием нарушения правила.</summary>
    public OperationForbiddenException(string message)
        : base(message)
    {
    }
}
