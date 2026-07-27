using Serilog;
using Serilog.Events;
using System.Reflection;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RaktApi.Application.Services;
using RaktApi.Infrastructure.Options;
using RaktWebApi.Services;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace RaktWebApi.Extensions;

/// <summary>
/// Методы расширения для стандартной настройки WebApplicationBuilder.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Регистрирует JWT-аутентификацию, авторизацию и контекст текущего пользователя.
    /// </summary>
    /// <param name="builder">Построитель веб-приложения.</param>
    /// <returns>Исходный построитель с настроенной безопасностью.</returns>
    /// <exception cref="InvalidOperationException">Секция JWT-настроек отсутствует.</exception>
    public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder)
    {
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Не задана секция конфигурации Jwt.");

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                        if (!Guid.TryParse(userIdValue, out _))
                        {
                            context.Fail("JWT-токен не содержит корректный идентификатор пользователя.");
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        builder.Services.AddAuthorization();

        return builder;
    }

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
        builder.Services.AddProblemDetails();

        // Swagger
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Введите JWT-токен без префикса Bearer.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });
        }

        return builder;
    }

    /// <summary>
    /// Настраивает Serilog как основной механизм логирования приложения.
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()


            .WriteTo.File(
                path: "logs/log-.txt", // log-2026-04-14.txt
                rollingInterval: RollingInterval.Day, // новый файл каждый день
                retainedFileCountLimit: 7, // храним 7 файлов
                fileSizeLimitBytes: 10_000_000, // 10 MB
                rollOnFileSizeLimit: true,
                shared: true,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] " +
                "[{SourceContext}] " +
                "{Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        builder.Host.UseSerilog();

        return builder;
    }
}
