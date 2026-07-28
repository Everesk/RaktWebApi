using Rakt.UsersService.Application;
using Rakt.UsersService.Domain;

namespace Rakt.UsersService.UnitTests;

/// <summary>
/// Выдаёт предсказуемый токен для unit-тестов сценариев пользователей.
/// </summary>
internal sealed class TestJwtTokenGenerator : IJwtTokenGenerator
{
    /// <summary>
    /// Возвращает строку с идентификатором пользователя вместо криптографического токена.
    /// </summary>
    public string Generate(User user)
    {
        return $"token-{user.Id}";
    }
}
