using Rakt.UsersService.Application;
using Rakt.UsersService.Infrastructure;
using Rakt.UsersService.Presentation.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
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

    var app = builder.Build();

    app.UseStandardConfiguration();
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
