using Rakt.EventsService.Application;
using Rakt.EventsService.Infrastructure;
using Rakt.EventsService.Presentation.Extensions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Запуск сервиса событий");

    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilogLogging();
    builder.AddStandardConfiguration();
    builder.AddJwtAuthentication();
    builder.Services.AddEventsApplication();
    builder.Services.AddEventsInfrastructure(builder.Configuration);
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(serviceName: "events-service"))
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
    Log.Fatal(exception, "Сервис событий был аварийно остановлен");
}
finally
{
    Log.CloseAndFlush();
}
