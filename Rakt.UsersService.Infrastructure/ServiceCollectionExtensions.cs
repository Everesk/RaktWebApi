using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rakt.UsersService.Application;
namespace Rakt.UsersService.Infrastructure;
/// <summary>Расширения для регистрации infrastructure-слоя пользователей.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Добавляет доступ к БД и реализации портов пользователей.</summary>
    public static IServiceCollection AddUsersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UsersDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("UsersDatabase")));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        return services;
    }
}
