using Microsoft.EntityFrameworkCore;
using Rakt.EventsService.Infrastructure;
namespace Rakt.EventsService.Presentation.Extensions;
/// <summary>Расширения для стандартного конвейера API событий.</summary>
public static class WebApplicationExtensions
{
    /// <summary>Применяет миграции, middleware и маршруты контроллеров.</summary>
    public static WebApplication UseStandardConfiguration(this WebApplication app)
    {
        using var scope = app.Services.CreateScope(); scope.ServiceProvider.GetRequiredService<EventsDbContext>().Database.Migrate();
        app.UseExceptionHandler(); if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
        app.UseHttpsRedirection(); app.MapControllers(); return app;
    }
}
