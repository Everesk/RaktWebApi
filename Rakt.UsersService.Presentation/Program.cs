using Rakt.UsersService.Application;
using Rakt.UsersService.Infrastructure;
using Rakt.UsersService.Presentation.Extensions;
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
    builder.AddTelemetry();

    var app = builder.Build();

    app.UseStandardConfiguration();
    app.MapTelemetryEndpoints();
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
