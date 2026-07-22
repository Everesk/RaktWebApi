namespace RaktWebApi.Common.Exceptions;

/// <summary>
/// Исключение для ситуаций, когда запрошенный ресурс не найден.
/// </summary>
public sealed class NotFoundException(string message) : Exception(message);