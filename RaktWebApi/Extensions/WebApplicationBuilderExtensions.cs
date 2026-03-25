using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using RaktWebApi.Common;

namespace RaktWebApi.Extensions;

/// <summary>
/// Методы расширения для стандартной настройки WebApplicationBuilder.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Применяет стандартную конфигурацию приложения:
    /// настройку DI-валидации, контроллеров и сваггер.
    /// </summary>
    public static WebApplicationBuilder AddStandardConfiguration(this WebApplicationBuilder builder)
    {
        builder.Host.UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
            options.ValidateOnBuild = context.HostingEnvironment.IsDevelopment();
        });

        builder.Services.AddControllers();

        // Ошибки в ProblemDetails
        builder.AddDefaultProblemDetails();

        // Swagger
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });
        }

        return builder;
    }

    /// <summary>
    /// Кастомная обработка ошибок валидации
    /// </summary>
    public static WebApplicationBuilder AddDefaultProblemDetails(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] =
                    context.HttpContext.TraceIdentifier;
            };
        });

        //Ошибки валидации
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problem = ProblemDetailsHelper.Validation(context.HttpContext, context.ModelState);
                return new BadRequestObjectResult(problem);
            };
        });

        return builder;
    }


}