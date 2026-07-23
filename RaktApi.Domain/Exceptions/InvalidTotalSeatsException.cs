namespace RaktApi.Domain.Exceptions;

/// <summary>
/// Исключение для ситуаций, когда событие создается с недопустимым количеством мест.
/// </summary>
public sealed class InvalidTotalSeatsException(string message) : Exception(message);
