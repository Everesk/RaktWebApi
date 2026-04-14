using Microsoft.AspNetCore.Mvc;
using RaktWebApi.Common;
namespace RaktWebApi.Controllers;

/// <summary>
/// Базовый класс контроллера - был нужен до ввода глобального обработчика исключений, сейчас не выполняет никакой функции, но может быть полезен для общих настроек контроллеров в будущем.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
}