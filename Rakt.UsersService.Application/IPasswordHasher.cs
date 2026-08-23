namespace Rakt.UsersService.Application;
/// <summary>Порт хеширования и проверки паролей.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
