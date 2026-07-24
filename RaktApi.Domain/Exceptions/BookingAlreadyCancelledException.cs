namespace RaktApi.Domain.Exceptions;

/// <summary>
/// Исключение при повторной отмене бронирования.
/// </summary>
public sealed class BookingAlreadyCancelledException(string message) : Exception(message);
