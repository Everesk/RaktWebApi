namespace RaktWebApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseStandardConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}