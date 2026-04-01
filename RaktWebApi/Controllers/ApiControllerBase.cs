using Microsoft.AspNetCore.Mvc;
using RaktWebApi.Common;
namespace RaktWebApi.Controllers;

/// <summary>
/// Базовый класс контроллера для работы с ProblemDetails
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Улучшаем стандартный 404
    /// </summary>
    protected NotFoundObjectResult NotFoundProblem(string detail) => NotFound(ProblemDetailsHelper.NotFound(HttpContext, detail));
}