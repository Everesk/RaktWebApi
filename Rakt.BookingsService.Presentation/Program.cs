using Rakt.BookingsService.Application;
using Rakt.BookingsService.Infrastructure;
using Rakt.BookingsService.Presentation.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
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

    var app = builder.Build();

    app.UseStandardConfiguration();
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
