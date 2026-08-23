using Microsoft.AspNetCore.Mvc;
namespace Rakt.UsersService.Presentation.Common;
/// <summary>Создаёт единообразные ответы об ошибках API пользователей.</summary>
public static class ProblemDetailsHelper
{
    /// <summary>Записывает Problem Details в HTTP-ответ.</summary>
    public static Task WriteAsync(HttpContext context, int statusCode, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(problem);
    }

    /// <summary>Возвращает описание ошибки по статусу.</summary>
    public static string GetDefaultDetail(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Запрос содержит некорректные данные.",
        StatusCodes.Status401Unauthorized => "Требуется действительный JWT-токен.",
        StatusCodes.Status403Forbidden => "У текущего пользователя недостаточно прав.",
        StatusCodes.Status404NotFound => "Запрошенный ресурс не найден.",
        _ => "При обработке запроса произошла ошибка."
    };

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Ошибка запроса",
        StatusCodes.Status401Unauthorized => "Требуется аутентификация",
        StatusCodes.Status403Forbidden => "Доступ запрещён",
        StatusCodes.Status404NotFound => "Ресурс не найден",
        _ => "Внутренняя ошибка сервера"
    };
}
