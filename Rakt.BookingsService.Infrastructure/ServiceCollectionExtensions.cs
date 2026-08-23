using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rakt.BookingsService.Application;
namespace Rakt.BookingsService.Infrastructure;
/// <summary>Расширения для регистрации infrastructure-слоя броней.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Добавляет доступ к БД и реализации портов броней.</summary>
    public static IServiceCollection AddBookingsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<BookingCreatedAtInterceptor>();
        services.AddDbContext<BookingsDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("BookingsDatabase"));
            options.AddInterceptors(serviceProvider.GetRequiredService<BookingCreatedAtInterceptor>());
        });

        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddSingleton<KafkaBookingMessagePublisher>();
        services.AddSingleton<IBookingMessagePublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<KafkaBookingMessagePublisher>());
        services.AddHostedService<SeatsReservationResultConsumer>();
        return services;
    }
}
