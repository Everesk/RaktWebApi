using Rakt.BookingsService.Application;
using Rakt.BookingsService.Infrastructure;
using Rakt.BookingsService.Presentation.Extensions;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("Запуск сервиса броней");

    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilogLogging();
    builder.AddStandardConfiguration();
    builder.AddJwtAuthentication();
    builder.Services.AddBookingsApplication();
    builder.Services.AddBookingsInfrastructure(builder.Configuration);
    builder.AddTelemetry();

    var app = builder.Build();

    app.UseStandardConfiguration();
    app.MapTelemetryEndpoints();
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Сервис броней был аварийно остановлен");
}
finally
{
    Log.CloseAndFlush();
}
