using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using Rakt.UsersService.Presentation.Common;
using Serilog;
using Rakt.UsersService.Infrastructure;
namespace Rakt.UsersService.Presentation.Extensions;
/// <summary>Расширения для стандартного конвейера API пользователей.</summary>
public static class WebApplicationExtensions
{
    /// <summary>Публикует endpoint Prometheus для сбора метрик.</summary>
    public static WebApplication MapTelemetryEndpoints(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint();

        return app;
    }

    /// <summary>Применяет миграции, middleware и маршруты контроллеров.</summary>
    public static WebApplication UseStandardConfiguration(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

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
