namespace Rakt.UsersService.Domain;

/// <summary>Пользователь, принадлежащий исключительно сервису пользователей.</summary>
public sealed class User
{
    /// <summary>Идентификатор пользователя.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();
    /// <summary>Уникальный логин пользователя.</summary>
    public string Login { get; private set; }
    /// <summary>Безопасный хеш пароля.</summary>
    public string PasswordHash { get; private set; }
    /// <summary>Роль пользователя.</summary>
    public UserRole Role { get; private set; }

    private User()
    {
        Login = null!;
        PasswordHash = null!;
    }

    private User(string login, string passwordHash, UserRole role)
    {
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }

    /// <summary>Создаёт пользователя с подготовленным хешем пароля.</summary>
    public static User Create(string login, string passwordHash, UserRole role = UserRole.User)
    {
        return new User(login, passwordHash, role);
    }
}
