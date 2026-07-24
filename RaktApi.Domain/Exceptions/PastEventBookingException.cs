namespace RaktApi.Domain.Exceptions;

/// <summary>
/// Исключение при попытке забронировать прошедшее событие.
/// </summary>
public sealed class PastEventBookingException(string message) : Exception(message);
