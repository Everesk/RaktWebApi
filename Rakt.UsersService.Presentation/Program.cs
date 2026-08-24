using Rakt.UsersService.Application;
using Rakt.UsersService.Infrastructure;
using Rakt.UsersService.Presentation.Extensions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("Запуск сервиса пользователей");

    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilogLogging();
    builder.AddStandardConfiguration();
    builder.AddJwtAuthentication();
    builder.Services.AddUsersApplication();
    builder.Services.AddUsersInfrastructure(builder.Configuration);
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(serviceName: "users-service"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(options =>
                options.Endpoint = new Uri(builder.Configuration["Otlp:Endpoint"]!)))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter());

    var app = builder.Build();

    app.UseStandardConfiguration();
    app.MapPrometheusScrapingEndpoint();
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Сервис пользователей был аварийно остановлен");
}
finally
{
    Log.CloseAndFlush();
}
