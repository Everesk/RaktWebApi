using Microsoft.EntityFrameworkCore;
using RaktApi.Application.Ports;
using RaktApi.Application.Services;
using RaktWebApi.Data;
using RaktWebApi.Data.Interceptors;
using RaktWebApi.Options;
using RaktWebApi.Repositories;
using RaktWebApi.Services;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace RaktWebApi.Extensions;

/// <summary>
/// Методы расширения для стандартной настройки WebApplicationBuilder.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Применяет стандартную конфигурацию приложения:
    /// настройку DI-валидации, контроллеров и сваггер.
    /// </summary>
    public static WebApplicationBuilder AddStandardConfiguration(this WebApplicationBuilder builder)
    {
        builder.Host.UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
            options.ValidateOnBuild = context.HostingEnvironment.IsDevelopment();
        });

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();

        // Swagger
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });
        }

        builder.AddServices();

        return builder;
    }

    private static WebApplicationBuilder AddServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<BookingProcessingOptions>()
            .Bind(builder.Configuration.GetSection(BookingProcessingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.AddEF();

        builder.Services.AddScoped<IEventService, EventService>();
        builder.Services.AddScoped<IBookingService, BookingService>();
        builder.Services.AddScoped<IEventRepository, EventRepository>();
        builder.Services.AddScoped<IBookingRepository, BookingRepository>();
        builder.Services.AddScoped<IBookingProcessor, BookingProcessor>();
        builder.Services.AddHostedService<BookingBackgroundService>();
        return builder;
    }

    /// <summary>
    /// Регистрирует EF Core интерсепторы приложения.
    /// </summary>
    /// <param name="builder">Построитель приложения.</param>
    private static WebApplicationBuilder AddEF(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<BookingCreatedAtInterceptor>();
        builder.Services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(sp.GetRequiredService<BookingCreatedAtInterceptor>());
        });

        return builder;
    }

    /// <summary>
    /// Настраивает Serilog как основной механизм логирования приложения.
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()


            .WriteTo.File(
                path: "logs/log-.txt", // log-2026-04-14.txt
                rollingInterval: RollingInterval.Day, // новый файл каждый день
                retainedFileCountLimit: 7, // храним 7 файлов
                fileSizeLimitBytes: 10_000_000, // 10 MB
                rollOnFileSizeLimit: true,
                shared: true,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] " +
                "[{SourceContext}] " +
                "{Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();
        
        builder.Host.UseSerilog();

        return builder;
    }
}
