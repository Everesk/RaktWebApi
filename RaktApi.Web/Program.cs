using RaktApi.Application.Extensions;
using RaktApi.Infrastructure.Extensions;
using RaktApi.Infrastructure.Options;
using RaktWebApi.Extensions;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Запуск приложения");

    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilogLogging();
    builder.AddStandardConfiguration();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Не задана секция конфигурации Jwt.");
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
        });
    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseStandardConfiguration();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Приложение было аварийно остановлено");
}
finally
{
    Log.CloseAndFlush();
}
