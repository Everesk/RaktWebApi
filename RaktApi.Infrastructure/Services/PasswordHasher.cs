using System.Security.Cryptography;
using System.Text;
using RaktApi.Application.Services;

namespace RaktApi.Infrastructure.Services;

/// <summary>
/// Хеширует пароли алгоритмом SHA-256.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    /// <inheritdoc />
    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    /// <inheritdoc />
    public bool Verify(string password, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(passwordHash);

        var passwordHashBytes = Convert.FromHexString(Hash(password));
        var expectedHashBytes = Convert.FromHexString(passwordHash);

        return CryptographicOperations.FixedTimeEquals(passwordHashBytes, expectedHashBytes);
    }
}
