using Microsoft.Extensions.DependencyInjection;
namespace Rakt.BookingsService.Application;
/// <summary>Расширения для регистрации application-слоя броней.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Добавляет сценарии броней в DI-контейнер.</summary>
    public static IServiceCollection AddBookingsApplication(this IServiceCollection services)
    {
        services.AddScoped<BookingService>();

        return services;
    }
}
