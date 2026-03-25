using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RaktWebApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseStandardConfiguration(this WebApplication app)
    {
        app.UseGlobalExceptionHandler(); // Глобальный обработчик исключений

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

                var problemDetails = new ProblemDetails
                {
                    Title = "Внутренняя ошибка сервера",
                    Detail = $"Произошла непредвиденная ошибка на сервере: {exception?.Message}",
                    Status = StatusCodes.Status500InternalServerError,
                    Type = null,
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = context.TraceIdentifier
                    }
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
            });
        });

        return app;
    }
}