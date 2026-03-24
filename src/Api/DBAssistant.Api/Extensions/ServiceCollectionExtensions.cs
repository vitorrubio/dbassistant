using DBAssistant.Api.Configuration;
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
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddApiConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var apiKeyOptions = new ApiKeyOptions
        {
            HeaderName = configuration[ApiKeyOptions.HeaderNameConfigurationKey] ?? ApiKeyOptions.DefaultHeaderName,
            HeaderValue = configuration[ApiKeyOptions.HeaderValueConfigurationKey] ?? string.Empty
        };

        if (apiKeyOptions.IsConfigured() is false)
        {
            throw new InvalidOperationException(
                $"Configure '{ApiKeyOptions.HeaderNameConfigurationKey}' and '{ApiKeyOptions.HeaderValueConfigurationKey}' before starting the API.");
        }

        services.AddSingleton(apiKeyOptions);
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
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = apiKeyOptions.HeaderName,
                Description = "Supply the API key required to access the assistant endpoints."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    []
                }
            });
        });

        return services;
    }
}
