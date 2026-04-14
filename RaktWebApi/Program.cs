using RaktWebApi.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Запуск приложения");

    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilogLogging();
    builder.AddStandardConfiguration();

    var app = builder.Build();

    app.UseStandardConfiguration();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Приложение было аварийно остановлено");
}
finally
{
    Log.CloseAndFlush();
}