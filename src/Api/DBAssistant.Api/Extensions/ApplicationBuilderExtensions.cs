using DBAssistant.Api.Middleware;

namespace DBAssistant.Api.Extensions;

/// <summary>
/// Provides application-builder extensions for configuring the HTTP pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures Swagger, authorization, and controller endpoint mapping for the API application.
    /// </summary>
    /// <param name="app">The web application being configured.</param>
    /// <returns>The same web application instance for chaining.</returns>
    public static WebApplication UseApiConfiguration(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }
}
