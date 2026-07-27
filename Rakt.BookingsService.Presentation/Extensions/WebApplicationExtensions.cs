using Microsoft.EntityFrameworkCore;
using Rakt.BookingsService.Infrastructure;
namespace Rakt.BookingsService.Presentation.Extensions;
/// <summary>Расширения для стандартного конвейера API броней.</summary>
public static class WebApplicationExtensions
{
    /// <summary>Применяет миграции, middleware и маршруты контроллеров.</summary>
    public static WebApplication UseStandardConfiguration(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

        dbContext.Database.Migrate();

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.MapControllers();

        return app;
    }
}
