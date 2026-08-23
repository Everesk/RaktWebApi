using Microsoft.AspNetCore.Mvc;
namespace Rakt.BookingsService.Presentation.Common;
/// <summary>Создаёт единообразные ответы об ошибках API броней.</summary>
public static class ProblemDetailsHelper
{
    /// <summary>Записывает Problem Details в HTTP-ответ.</summary>
    public static Task WriteAsync(HttpContext context, int statusCode, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Ошибка",
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(problem);
    }

    /// <summary>Возвращает описание ошибки по статусу.</summary>
    public static string GetDefaultDetail(int statusCode)
    {
        return statusCode == StatusCodes.Status404NotFound
            ? "Запрошенный ресурс не найден."
            : "При обработке запроса произошла ошибка.";
    }
}
