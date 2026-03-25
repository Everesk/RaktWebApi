namespace RaktWebApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseStandardConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}