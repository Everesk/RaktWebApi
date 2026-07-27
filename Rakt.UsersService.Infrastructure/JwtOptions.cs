namespace Rakt.UsersService.Infrastructure;
/// <summary>Настройки JWT сервиса пользователей.</summary>
public sealed class JwtOptions
{
    /// <summary>Секрет подписи.</summary>
    public string Secret { get; init; } = null!;
    /// <summary>Издатель токена.</summary>
    public string Issuer { get; init; } = null!;
    /// <summary>Получатель токена.</summary>
    public string Audience { get; init; } = null!;
    /// <summary>Время жизни в минутах.</summary>
    public int LifetimeMinutes { get; init; } = 60;
}
