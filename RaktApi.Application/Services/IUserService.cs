using RaktApi.Application.DTO;
using RaktApi.Domain;

namespace RaktApi.Application.Services;

/// <summary>Предоставляет сценарии регистрации и аутентификации пользователей.</summary>
public interface IUserService
{
    /// <summary>Регистрирует нового обычного пользователя.</summary>
    Task<User> RegisterAsync(RegisterUserDto dto, CancellationToken cancellationToken = default);

    /// <summary>Проверяет учетные данные и возвращает JWT-токен.</summary>
    Task<AuthenticationDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
}
