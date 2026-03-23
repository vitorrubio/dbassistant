namespace DBAssistant.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiConfiguration(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }
}
