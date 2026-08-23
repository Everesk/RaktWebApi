namespace Rakt.UsersService.Domain.Exceptions;

/// <summary>Возникает при регистрации пользователя с уже занятым логином.</summary>
public sealed class UserAlreadyExistsException : Exception
{
    /// <summary>Создаёт исключение с описанием нарушения правила.</summary>
    public UserAlreadyExistsException(string message)
        : base(message)
    {
    }
}
