using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaktApi.Application.Ports;
using RaktApi.Infrastracture.Data;
using RaktApi.Infrastracture.Data.Interceptors;
using RaktApi.Infrastracture.Repositories;

namespace RaktApi.Infrastracture.Extensions;

/// <summary>
/// Методы расширения для регистрации зависимостей инфраструктурного слоя.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует контекст базы данных, EF Core-интерсепторы и реализации портов репозиториев.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configuration">Конфигурация с параметрами подключения к внешним системам.</param>
    /// <returns>Исходная коллекция сервисов с зарегистрированными инфраструктурными зависимостями.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<BookingCreatedAtInterceptor>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(serviceProvider.GetRequiredService<BookingCreatedAtInterceptor>());
        });

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        return services;
    }

    /// <summary>
    /// Применяет все непримененные миграции базы данных инфраструктурного слоя.
    /// </summary>
    /// <param name="serviceProvider">Корневой поставщик сервисов приложения.</param>
    public static void ApplyInfrastructureMigrations(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
    }
}
