using Microsoft.EntityFrameworkCore;
using Rakt.EventsService.Infrastructure;
using Rakt.EventsService.Presentation.Common;
using Serilog;
namespace Rakt.EventsService.Presentation.Extensions;
/// <summary>Расширения для стандартного конвейера API событий.</summary>
public static class WebApplicationExtensions
{
    /// <summary>Применяет миграции, middleware и маршруты контроллеров.</summary>
    public static WebApplication UseStandardConfiguration(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        dbContext.Database.Migrate();

        app.UseSerilogRequestLogging();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseStatusCodePages(async context =>
        {
            var httpContext = context.HttpContext;
            var statusCode = httpContext.Response.StatusCode;

            await ProblemDetailsHelper.WriteAsync(
                httpContext,
                statusCode,
                ProblemDetailsHelper.GetDefaultDetail(statusCode));
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
