namespace RaktApi.Domain.Exceptions;

/// <summary>
/// Исключение при превышении лимита активных бронирований пользователя.
/// </summary>
public sealed class ActiveBookingsLimitExceededException(string message) : Exception(message);
