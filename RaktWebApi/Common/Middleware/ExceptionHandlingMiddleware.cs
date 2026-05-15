using RaktWebApi.Common.Exceptions;
using RaktWebApi.Common.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace RaktWebApi.Common.Middleware;

/// <summary>
/// Middleware для глобальной обработки необработанных исключений.
/// Перехватывает исключения и возвращает клиенту единообразный ответ.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>
    /// Выполняет следующий middleware в pipeline и перехватывает исключения.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    /// <summary>
    /// Обрабатывает исключение: логирует и делегирует формирование ответа helper'у.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);

        // Логирование
        if (statusCode >= 500)
        {
            logger.LogError(exception,
                "Необработанная ошибка {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }
        else
        {
            //тут без стек трейса, так как это ожидаемые ошибки
            logger.LogWarning(
                "Ошибка запроса {Method} {Path}: {Message}",
                context.Request.Method,
                context.Request.Path,
                exception.Message);
        }

        await ProblemDetailsHelper.WriteProblemDetailsAsync(
            context,
            statusCode,
            exception.Message);
    }

    /// <summary>
    /// Определяет HTTP-статус по типу исключения.
    /// </summary>
    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            NoAvailableSeatsException => (int)HttpStatusCode.Conflict,
            ValidationException => (int)HttpStatusCode.BadRequest,
            NotFoundException => (int)HttpStatusCode.NotFound,
            BadHttpRequestException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }
}
