namespace Rakt.UsersService.Presentation.Common;

using Rakt.UsersService.Domain.Exceptions;
/// <summary>Перехватывает исключения и возвращает единый ответ Problem Details.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>Выполняет следующий middleware и обрабатывает необработанные исключения.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var statusCode = exception switch
            {
                UserAlreadyExistsException => StatusCodes.Status409Conflict,
                InvalidCredentialsException => StatusCodes.Status404NotFound,
                BadHttpRequestException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            logger.LogError(exception, "Ошибка обработки {Method} {Path}", context.Request.Method, context.Request.Path);
            await ProblemDetailsHelper.WriteAsync(context, statusCode, exception.Message);
        }
    }
}
