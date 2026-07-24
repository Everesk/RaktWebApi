namespace RaktApi.Domain.Exceptions;

/// <summary>Исключение при неверных учетных данных пользователя.</summary>
public sealed class InvalidCredentialsException(string message) : Exception(message);
