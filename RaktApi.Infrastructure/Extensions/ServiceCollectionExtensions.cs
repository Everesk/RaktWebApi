using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaktApi.Application.Ports;
using RaktApi.Application.Services;
using RaktApi.Application.Options;
using RaktApi.Infrastructure.Data;
using RaktApi.Infrastructure.Data.Interceptors;
using RaktApi.Infrastructure.BackgroundServices;
using RaktApi.Infrastructure.Options;
using RaktApi.Infrastructure.Repositories;
using RaktApi.Infrastructure.Services;

namespace RaktApi.Infrastructure.Extensions;

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

        services.AddOptions<BookingProcessingOptions>()
            .Bind(configuration.GetSection(BookingProcessingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHostedService<BookingBackgroundService>();

        services.AddOptions<BookingOptions>()
            .Bind(configuration.GetSection(BookingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

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
