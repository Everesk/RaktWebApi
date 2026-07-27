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
        services.AddDbContext<BookingsDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("BookingsDatabase")));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddSingleton<IBookingConfirmedPublisher, KafkaBookingConfirmedPublisher>();
        return services;
    }
}
