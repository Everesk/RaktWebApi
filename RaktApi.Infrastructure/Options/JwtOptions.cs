using System.ComponentModel.DataAnnotations;

namespace RaktApi.Infrastructure.Options;

/// <summary>
/// Настройки подписи и времени жизни JWT-токенов.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Секретный ключ для подписи токенов.
    /// </summary>
    [Required]
    [MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Издатель токена.
    /// </summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Аудитория токена.
    /// </summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Время жизни токена в минутах.
    /// </summary>
    [Range(1, 1440)]
    public int LifetimeMinutes { get; set; } = 60;
}
