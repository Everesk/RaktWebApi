using Microsoft.AspNetCore.Mvc;

namespace RaktWebApi.Common.Helpers;

/// <summary>
/// Хелпер для формирования единообразных ответов ProblemDetails.
/// </summary>
public static class ProblemDetailsHelper
{
    /// <summary>
    /// Записывает ProblemDetails в HTTP-ответ.
    /// </summary>
    public static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = detail,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    /// <summary>
    /// Возвращает стандартное описание ошибки по статус-коду.
    /// </summary>
    public static string GetDefaultDetail(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status404NotFound => "Запрошенный ресурс не найден.",
            StatusCodes.Status405MethodNotAllowed => "HTTP-метод не поддерживается для данного маршрута.",
            StatusCodes.Status400BadRequest => "Запрос содержит некорректные данные.",
            StatusCodes.Status500InternalServerError => "На сервере произошла непредвиденная ошибка.",
            _ => "При обработке запроса произошла ошибка."
        };
    }

    /// <summary>
    /// Возвращает заголовок ошибки (кастомной) по статус-коду.
    /// </summary>
    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Ошибка запроса",
            StatusCodes.Status404NotFound => "Ресурс не найден",
            StatusCodes.Status405MethodNotAllowed => "Метод не поддерживается",
            StatusCodes.Status500InternalServerError => "Внутренняя ошибка сервера",
            _ => "Ошибка"
        };
    }
}