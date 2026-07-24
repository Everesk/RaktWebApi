namespace RaktApi.Domain.Exceptions;

/// <summary>
/// Исключение при отсутствии прав на выполнение операции.
/// </summary>
public sealed class OperationForbiddenException(string message) : Exception(message);
