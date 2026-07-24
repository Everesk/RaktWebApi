using FluentAssertions;
using RaktApi.Infrastructure.Services;

namespace Rakt.Tests.Services;

/// <summary>
/// Набор тестов для <see cref="PasswordHasher"/>.
/// </summary>
public class PasswordHasherTests
{
    /// <summary>
    /// Проверяет вычисление SHA-256 хеша для пароля.
    /// </summary>
    [Fact]
    public void Hash_ShouldReturnSha256Hash()
    {
        // Arrange
        var passwordHasher = new PasswordHasher();

        // Act
        var passwordHash = passwordHasher.Hash("password");

        // Assert
        passwordHash.Should().Be("5E884898DA28047151D0E56F8DC6292773603D0D6AABBDD62A11EF721D1542D8");
    }

    /// <summary>
    /// Проверяет соответствие пароля корректному хешу.
    /// </summary>
    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordMatchesHash()
    {
        // Arrange
        var passwordHasher = new PasswordHasher();
        var passwordHash = passwordHasher.Hash("password");

        // Act
        var isMatch = passwordHasher.Verify("password", passwordHash);

        // Assert
        isMatch.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет отсутствие соответствия для другого пароля.
    /// </summary>
    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordDoesNotMatchHash()
    {
        // Arrange
        var passwordHasher = new PasswordHasher();
        var passwordHash = passwordHasher.Hash("password");

        // Act
        var isMatch = passwordHasher.Verify("another-password", passwordHash);

        // Assert
        isMatch.Should().BeFalse();
    }
}
