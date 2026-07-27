using Rakt.UsersService.Domain;
namespace Rakt.UsersService.Application;
/// <summary>Данные регистрации пользователя.</summary>
public sealed record RegisterUserCommand(string Login, string Password, UserRole Role = UserRole.User);
