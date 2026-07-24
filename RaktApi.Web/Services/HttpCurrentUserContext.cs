using RaktApi.Application.Services;
using RaktApi.Domain;
using RaktWebApi.Extensions;

namespace RaktWebApi.Services;

/// <summary>
/// Получает данные текущего пользователя из claims HTTP-запроса.
/// </summary>
public sealed class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    /// <inheritdoc />
    public Guid UserId => httpContextAccessor.HttpContext?.User.GetUserId()
        ?? throw new InvalidOperationException("Контекст HTTP-запроса недоступен.");

    /// <inheritdoc />
    public UserRole Role => httpContextAccessor.HttpContext?.User.IsInRole(nameof(UserRole.Admin)) == true
        ? UserRole.Admin
        : UserRole.User;
}
