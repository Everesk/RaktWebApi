using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RaktWebApi.Common;

/// <summary>
/// Для лаконичного создания ProblemDetails
/// </summary>
public static class ProblemDetailsHelper
{
    /// <summary>
    /// Создает ProblemDetails в едином формате
    /// </summary>
    
    public static ProblemDetails Create(HttpContext context, int statusCode, string title, string? detail = null)
    {
        var problem = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Type = null,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };

        return problem;
    }

    /// <summary>
    /// Создает ProblemDetails ошибки валидации
    /// </summary>
    public static ValidationProblemDetails Validation(HttpContext context, ModelStateDictionary modelState)
    {
        var problem = new ValidationProblemDetails(modelState)
        {
            Title = "Ошибка валидации",
            Status = StatusCodes.Status400BadRequest,
            Type = null,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };

        return problem;
    }

    /// <summary>
    /// Создает ProblemDetails ошибки поиска ресурса (404)
    /// </summary>
    public static ProblemDetails NotFound(HttpContext context, string? detail = null)
        => Create(context, StatusCodes.Status404NotFound, "Ресурс не найден", detail);

    /// <summary>
    /// Создает ProblemDetails внутренней ошибки сервера (500)
    /// </summary>
    public static ProblemDetails InternalError(HttpContext context, Exception? ex)
        => Create(context, StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера",
            $"Произошла непредвиденная ошибка на сервере: {ex?.Message}");


}