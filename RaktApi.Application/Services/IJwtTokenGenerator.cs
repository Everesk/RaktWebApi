using RaktApi.Domain;

namespace RaktApi.Application.Services;

/// <summary>
/// Формирует JWT-токены для пользователей системы.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Создает подписанный JWT-токен пользователя.
    /// </summary>
    /// <param name="user">Пользователь, для которого создается токен.</param>
    /// <returns>Строковое представление подписанного JWT-токена.</returns>
    string Generate(User user);
}
