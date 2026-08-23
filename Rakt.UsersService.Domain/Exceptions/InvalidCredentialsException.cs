namespace Rakt.UsersService.Domain.Exceptions;

/// <summary>Возникает при передаче неверных учётных данных.</summary>
public sealed class InvalidCredentialsException : Exception
{
    /// <summary>Создаёт исключение с описанием нарушения правила.</summary>
    public InvalidCredentialsException(string message)
        : base(message)
    {
    }
}
