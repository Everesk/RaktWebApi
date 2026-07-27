using Rakt.EventsService.Application;
using Rakt.EventsService.Infrastructure;
using Rakt.EventsService.Presentation.Extensions;
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

    var app = builder.Build();

    app.UseStandardConfiguration();
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
