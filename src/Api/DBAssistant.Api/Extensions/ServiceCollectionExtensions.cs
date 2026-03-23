using Microsoft.OpenApi.Models;

namespace DBAssistant.Api.Extensions;

/// <summary>
/// Provides service-registration extensions for the API layer.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds controllers and Swagger services required by the API layer.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddApiConfiguration(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DB Assistant API",
                Version = "v1",
                Description = "API for translating natural language into safe read-only MySQL queries."
            });
        });

        return services;
    }
}
