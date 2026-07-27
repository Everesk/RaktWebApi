namespace Rakt.UsersService.Application;
/// <summary>Данные входа пользователя.</summary>
public sealed record LoginCommand(string Login, string Password);
