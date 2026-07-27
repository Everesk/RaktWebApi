using Microsoft.Extensions.DependencyInjection;
namespace Rakt.EventsService.Application;
/// <summary>Расширения для регистрации application-слоя событий.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Добавляет сценарии событий в DI-контейнер.</summary>
    public static IServiceCollection AddEventsApplication(this IServiceCollection services) => services.AddScoped<EventSeatsService>();
}
