using Microsoft.Extensions.DependencyInjection;
namespace Rakt.UsersService.Application;
/// <summary>Расширения для регистрации application-слоя пользователей.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Добавляет сценарии пользователей в DI-контейнер.</summary>
    public static IServiceCollection AddUsersApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
