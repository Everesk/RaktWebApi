using System.ComponentModel.DataAnnotations;

namespace RaktApi.Application.DTO;

/// <summary>Данные для регистрации пользователя.</summary>
public sealed class RegisterUserDto
{
    /// <summary>Логин нового пользователя.</summary>
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = string.Empty;

    /// <summary>Пароль нового пользователя.</summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Данные для входа пользователя.</summary>
public sealed class LoginDto
{
    /// <summary>Логин пользователя.</summary>
    [Required]
    public string Login { get; set; } = string.Empty;

    /// <summary>Пароль пользователя.</summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Результат успешной аутентификации.</summary>
public sealed class AuthenticationDto
{
    /// <summary>Подписанный JWT-токен.</summary>
    public string Token { get; init; } = string.Empty;
}
