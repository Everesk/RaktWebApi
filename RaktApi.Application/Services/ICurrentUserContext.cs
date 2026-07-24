using RaktApi.Domain;

namespace RaktApi.Application.Services;

/// <summary>
/// Предоставляет данные аутентифицированного пользователя, выполняющего текущую операцию.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>
    /// Идентификатор текущего пользователя.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Роль текущего пользователя.
    /// </summary>
    UserRole Role { get; }
}
