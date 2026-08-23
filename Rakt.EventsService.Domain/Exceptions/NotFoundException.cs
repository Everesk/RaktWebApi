namespace Rakt.EventsService.Domain.Exceptions;

/// <summary>Возникает при отсутствии запрошенного ресурса сервиса событий.</summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Создаёт исключение с описанием отсутствующего ресурса.</summary>
    public NotFoundException(string message)
        : base(message)
    {
    }
}
