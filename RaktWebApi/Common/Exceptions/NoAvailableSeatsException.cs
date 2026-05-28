namespace RaktWebApi.Common.Exceptions;

/// <summary>
/// Исключение для ситуаций, когда на событие больше нет свободных мест.
/// </summary>
public sealed class NoAvailableSeatsException(string message) : Exception(message);
