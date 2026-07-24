using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RaktWebApi.Extensions;

/// <summary>
/// Методы извлечения данных текущего пользователя из JWT-claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Возвращает идентификатор аутентифицированного пользователя.
    /// </summary>
    /// <param name="user">Пользователь, сформированный middleware аутентификации.</param>
    /// <returns>Идентификатор пользователя из токена.</returns>
    /// <exception cref="InvalidOperationException">Токен не содержит корректного идентификатора пользователя.</exception>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : throw new InvalidOperationException("JWT-токен не содержит корректный идентификатор пользователя.");
    }
}
