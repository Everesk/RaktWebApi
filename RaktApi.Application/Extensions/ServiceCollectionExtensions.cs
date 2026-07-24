using Microsoft.Extensions.DependencyInjection;
using RaktApi.Application.Services;

namespace RaktApi.Application.Extensions;

/// <summary>
/// Методы расширения для регистрации use cases слоя Application.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует сервисы и обработчики use cases приложения.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <returns>Исходная коллекция сервисов с зарегистрированными use cases.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingProcessor, BookingProcessor>();
        services.AddSingleton<IBookingProcessingState, BookingProcessingState>();
        services.AddScoped<IBookingProcessingService, BookingProcessingService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
