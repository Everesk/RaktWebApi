using RaktApi.Application.Services;
using RaktApi.Domain;

namespace Rakt.Tests.Infrastructure;

/// <summary>
/// Управляемый контекст текущего пользователя для unit-тестов.
/// </summary>
public sealed class TestCurrentUserContext : ICurrentUserContext
{
    /// <summary>
    /// Идентификатор текущего пользователя в тесте.
    /// </summary>
    public Guid UserId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Роль текущего пользователя в тесте.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;
}
