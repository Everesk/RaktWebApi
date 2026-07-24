using RaktApi.Domain;

namespace RaktApi.Application.Ports;

/// <summary>Предоставляет доступ к данным пользователей.</summary>
public interface IUserRepository
{
    /// <summary>Возвращает пользователя по логину.</summary>
    Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);

    /// <summary>Добавляет пользователя и сохраняет изменения.</summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
