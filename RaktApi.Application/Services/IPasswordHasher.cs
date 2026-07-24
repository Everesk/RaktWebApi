namespace RaktApi.Application.Services;

/// <summary>
/// Предоставляет операции хеширования и проверки паролей.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Возвращает SHA-256 хеш пароля в шестнадцатеричном представлении.
    /// </summary>
    /// <param name="password">Исходный пароль.</param>
    /// <returns>Хеш пароля.</returns>
    string Hash(string password);

    /// <summary>
    /// Проверяет соответствие пароля сохраненному хешу.
    /// </summary>
    /// <param name="password">Исходный пароль.</param>
    /// <param name="passwordHash">Сохраненный хеш пароля.</param>
    /// <returns><see langword="true" />, если пароль соответствует хешу; иначе <see langword="false" />.</returns>
    bool Verify(string password, string passwordHash);
}
