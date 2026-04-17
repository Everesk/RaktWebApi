using RaktWebApi.Common.Helpers;
using RaktWebApi.Common.Middleware;
using Serilog;

namespace RaktWebApi.Extensions;

/// <summary>
/// Упрощает код Program.cs
/// </summary>
public static class WebApplicationExtensions
{

    /// <summary>
    /// Стандартные настройки приложения
    /// </summary>
    public static WebApplication UseStandardConfiguration(this WebApplication app)
    {
        // Serilog-логирование HTTP-запросов.
        app.UseSerilogRequestLogging();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Преобразуем также пустые 4xx/5xx ответы без body, например 404 и 405
        app.UseStatusCodePages(async context =>
        {
            var httpContext = context.HttpContext;
            var statusCode = httpContext.Response.StatusCode;

            await ProblemDetailsHelper.WriteProblemDetailsAsync(
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
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}