namespace RaktApi.Domain.Exceptions;

/// <summary>Исключение при регистрации пользователя с занятым логином.</summary>
public sealed class UserAlreadyExistsException(string message) : Exception(message);
