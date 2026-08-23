using System.Security.Cryptography;
using System.Text;
using Rakt.UsersService.Application;
namespace Rakt.UsersService.Infrastructure;
/// <summary>Хеширует пароли алгоритмом SHA-256.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
    public bool Verify(string password, string hash) => CryptographicOperations.FixedTimeEquals(Convert.FromHexString(Hash(password)), Convert.FromHexString(hash));
}
