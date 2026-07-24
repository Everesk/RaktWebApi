namespace RaktApi.Domain;

/// <summary>
/// Представляет пользователя системы.
/// </summary>
public class User
{
    /// <summary>
    /// Навигационная коллекция бронирований пользователя.
    /// </summary>
    public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();

    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Логин пользователя.
    /// </summary>
    public string Login { get; private set; }

    /// <summary>
    /// Хеш пароля пользователя.
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// Роль пользователя в системе.
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Инициализирует пользователя указанными данными.
    /// </summary>
    internal User(string login, string passwordHash, UserRole role)
    {
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }

    /// <summary>
    /// Создает нового пользователя.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="passwordHash">Хеш пароля пользователя.</param>
    /// <param name="role">Роль пользователя.</param>
    /// <returns>Новый пользователь.</returns>
    public static User Create(string login, string passwordHash, UserRole role = UserRole.User)
    {
        return new User(login, passwordHash, role);
    }

    /// <summary>
    /// Приватный конструктор для материализации сущности.
    /// </summary>
    private User()
    {
        Login = null!;
        PasswordHash = null!;
    }
}
