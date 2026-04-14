using Microsoft.AspNetCore.Diagnostics;
using RaktWebApi.Common;
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

        app.UseGlobalExceptionHandler(); // Глобальный обработчик исключений
        app.UseDefaultStatusCodePages(); // Переопределим некоторые статусные ошибки на ProblemDetails

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
    /// <summary>
    /// Добавляет глобальный обработчик исключений, который перехватывает все необработанные исключения,
    /// </summary>
    public static WebApplication UseGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionFeature?.Error;

                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GlobalExceptionHandler");

                logger.LogError(
                    exception,
                    "Необработанное исключение. TraceId: {TraceId}. Message: {Message}",
                    context.TraceIdentifier, exception?.Message);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                var problemDetails = ProblemDetailsHelper.InternalError(context, exception);
                await context.Response.WriteAsJsonAsync(problemDetails);
            });
        });

        return app;
    }

    /// <summary>
    /// Добавляем ProblemDetails для некоторых кодов ошибок.
    /// </summary>
    public static WebApplication UseDefaultStatusCodePages(this WebApplication app)
    {
        app.UseStatusCodePages(async statusCodeContext =>
        {
            var httpContext = statusCodeContext.HttpContext;
            var response = httpContext.Response;

            if (response.StatusCode is not (
                StatusCodes.Status404NotFound or
                StatusCodes.Status405MethodNotAllowed))
            {
                return;
            }

            response.ContentType = "application/problem+json";

            var problemDetails = response.StatusCode switch
            {
                StatusCodes.Status404NotFound =>
                    ProblemDetailsHelper.NotFound(httpContext),

                StatusCodes.Status405MethodNotAllowed =>
                    ProblemDetailsHelper.Create(httpContext,StatusCodes.Status405MethodNotAllowed, "Метод не поддерживается"),

                _ => null
            };

            if (problemDetails is null)
                return;

            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

            await response.WriteAsJsonAsync(problemDetails);
        });

        return app;
    }
}