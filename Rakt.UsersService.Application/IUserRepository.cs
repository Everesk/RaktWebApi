using Rakt.UsersService.Domain;
namespace Rakt.UsersService.Application;
/// <summary>Порт хранения пользователей.</summary>
public interface IUserRepository { Task<User?> FindByLoginAsync(string login, CancellationToken cancellationToken = default); Task AddAsync(User user, CancellationToken cancellationToken = default); }
