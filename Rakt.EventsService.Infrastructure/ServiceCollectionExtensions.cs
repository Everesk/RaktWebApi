using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rakt.EventsService.Application;
namespace Rakt.EventsService.Infrastructure;
/// <summary>Расширения для регистрации infrastructure-слоя событий.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Добавляет доступ к БД и реализации портов событий.</summary>
    public static IServiceCollection AddEventsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("EventsDatabase")));
        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddSingleton<KafkaSeatsReservationPublisher>();
        services.AddSingleton<ISeatsReservationPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<KafkaSeatsReservationPublisher>());
        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<BookingRequestedConsumer>();
        services.AddHostedService<BookingCancelledConsumer>();

        return services;
    }
}
