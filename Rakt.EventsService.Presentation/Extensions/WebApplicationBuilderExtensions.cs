using System.Reflection;
namespace Rakt.EventsService.Presentation.Extensions;
/// <summary>Расширения для стандартной настройки построителя API событий.</summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>Регистрирует контроллеры, Problem Details и Swagger.</summary>
    public static WebApplicationBuilder AddStandardConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml")));
        return builder;
    }
}
